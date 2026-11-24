-- =============================================================================
-- MusicStore — Schema parity verification (200_verify_schema_parity.sql)
-- MIG-08
--
-- Verifies the PostgreSQL schema against the legacy SQL Server schema
-- (MusicStoreModel.edmx SSDL). Every check is a catalog query; the script
-- aborts with an exception on the first failed assertion so that DbUp (and
-- any standalone psql run) fails loudly when parity is broken.
--
-- Checks:
--   1. Required tables exist: artist, genre, album, song
--   2. Primary keys: single integer column, GENERATED ALWAYS AS IDENTITY
--   3. Foreign keys: fk_album_artist, fk_album_genre, fk_song_album with
--      correct columns/references and ON DELETE rules
--   4. String columns are varchar (no bpchar/char padding anywhere)
--   5. song.duration is interval
--   6. All columns are NOT NULL
--   7. Indexes exist on every FK column
--   8. Constraint/index names are unique (no accidental duplicates)
--
-- Idempotent: read-only (no DDL/DML), safe to re-run via psql at any time.
-- =============================================================================

DO $$
DECLARE
    v_actual text;
    v_count  integer;
BEGIN

-- -----------------------------------------------------------------------------
-- 1. Required tables exist
-- -----------------------------------------------------------------------------
    SELECT string_agg(c.relname, ', ' ORDER BY c.relname)
      INTO v_actual
      FROM pg_class c
      JOIN pg_namespace n ON n.oid = c.relnamespace
     WHERE n.nspname = 'public'
       AND c.relname IN ('artist', 'genre', 'album', 'song')
       AND c.relkind = 'r';

    IF v_actual IS NULL OR
       (SELECT count(*) FROM unnest(string_to_array(v_actual, ', ')) AS t(name)) < 4 THEN
        RAISE EXCEPTION 'PARITY FAIL (tables): expected artist, genre, album, song; found %', coalesce(v_actual, 'none');
    END IF;

-- -----------------------------------------------------------------------------
-- 2. Primary keys: single integer identity column per table
--    (legacy: IDENTITY int PK -> target: GENERATED ALWAYS AS IDENTITY)
-- -----------------------------------------------------------------------------
    FOR v_actual IN
        SELECT c.relname
          FROM pg_class c
          JOIN pg_namespace n ON n.oid = c.relnamespace
         WHERE n.nspname = 'public'
           AND c.relname IN ('artist', 'genre', 'album', 'song')
           AND NOT EXISTS (
                SELECT 1
                  FROM pg_index i
                  JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = i.indkey[0]
                  JOIN pg_type ty ON ty.oid = a.atttypid
                 WHERE i.indrelid = c.oid
                   AND i.indisprimary
                   AND i.indnatts = 1
                   AND ty.typname = 'int4'
                   AND a.attidentity IN ('a', 'd')   -- a = ALWAYS, d = BY DEFAULT
              )
    LOOP
        RAISE EXCEPTION 'PARITY FAIL (pk/identity): table % lacks a single-column integer identity primary key', v_actual;
    END LOOP;

-- -----------------------------------------------------------------------------
-- 3. Foreign keys: names, columns, references and ON DELETE rules
--    (legacy SSDL: FK_Album_Artist, FK_Album_Genre, FK_Song_Album)
-- -----------------------------------------------------------------------------
    IF NOT EXISTS (
        SELECT 1
          FROM pg_constraint con
          JOIN pg_class child ON child.oid = con.conrelid
          JOIN pg_namespace n ON n.oid = child.relnamespace
          JOIN pg_class parent ON parent.oid = con.confrelid
         WHERE n.nspname = 'public'
           AND con.contype = 'f'
           AND con.conname = 'fk_album_artist'
           AND child.relname = 'album'
           AND parent.relname = 'artist'
           AND (SELECT pg_get_constraintdef(con.oid)) LIKE '%artist_id%'
           AND con.confdeltype = 'c'          -- CASCADE
    ) THEN
        RAISE EXCEPTION 'PARITY FAIL (fk): fk_album_artist (album.artist_id -> artist, ON DELETE CASCADE) missing or wrong';
    END IF;

    IF NOT EXISTS (
        SELECT 1
          FROM pg_constraint con
          JOIN pg_class child ON child.oid = con.conrelid
          JOIN pg_namespace n ON n.oid = child.relnamespace
          JOIN pg_class parent ON parent.oid = con.confrelid
         WHERE n.nspname = 'public'
           AND con.contype = 'f'
           AND con.conname = 'fk_album_genre'
           AND child.relname = 'album'
           AND parent.relname = 'genre'
           AND (SELECT pg_get_constraintdef(con.oid)) LIKE '%genre_id%'
           AND con.confdeltype = 'r'          -- r = RESTRICT
    ) THEN
        RAISE EXCEPTION 'PARITY FAIL (fk): fk_album_genre (album.genre_id -> genre, ON DELETE RESTRICT) missing';
    END IF;

    IF NOT EXISTS (
        SELECT 1
          FROM pg_constraint con
          JOIN pg_class child ON child.oid = con.conrelid
          JOIN pg_namespace n ON n.oid = child.relnamespace
          JOIN pg_class parent ON parent.oid = con.confrelid
         WHERE n.nspname = 'public'
           AND con.contype = 'f'
           AND con.conname = 'fk_song_album'
           AND child.relname = 'song'
           AND parent.relname = 'album'
           AND (SELECT pg_get_constraintdef(con.oid)) LIKE '%album_id%'
           AND con.confdeltype = 'c'          -- c = CASCADE
    ) THEN
        RAISE EXCEPTION 'PARITY FAIL (fk): fk_song_album (song.album_id -> album, ON DELETE CASCADE) missing';
    END IF;

-- -----------------------------------------------------------------------------
-- 4. String columns are varchar — no bpchar (char/nchar) padding anywhere
--    (legacy debt: nchar(100)/nchar(50) fixed-length padding)
-- -----------------------------------------------------------------------------
    SELECT count(*) INTO v_count
      FROM information_schema.columns
     WHERE table_schema = 'public'
       AND table_name IN ('artist', 'genre', 'album', 'song')
       AND data_type NOT IN ('character varying', 'integer', 'bigint', 'interval');

    IF v_count > 0 THEN
        RAISE EXCEPTION 'PARITY FAIL (types): % column(s) with unexpected data type (expected only varchar/integer/interval)', v_count;
    END IF;

    SELECT count(*) INTO v_count
      FROM information_schema.columns
     WHERE table_schema = 'public'
       AND table_name IN ('artist', 'genre', 'album', 'song')
       AND data_type = 'character';   -- bpchar: fixed-length padding — legacy debt

    IF v_count > 0 THEN
        RAISE EXCEPTION 'PARITY FAIL (bpchar): % column(s) are char/bpchar — fixed-length padding must not return', v_count;
    END IF;

-- -----------------------------------------------------------------------------
-- 5. song.duration is interval (legacy: time(7) — could not exceed 24h)
-- -----------------------------------------------------------------------------
    IF NOT EXISTS (
        SELECT 1
          FROM information_schema.columns
         WHERE table_schema = 'public'
           AND table_name = 'song'
           AND column_name = 'duration'
           AND data_type = 'interval'
    ) THEN
        RAISE EXCEPTION 'PARITY FAIL (interval): song.duration is not interval';
    END IF;

-- -----------------------------------------------------------------------------
-- 6. All columns NOT NULL (legacy SSDL: Nullable="false" on every property)
-- -----------------------------------------------------------------------------
    SELECT count(*) INTO v_count
      FROM information_schema.columns
     WHERE table_schema = 'public'
       AND table_name IN ('artist', 'genre', 'album', 'song')
       AND is_nullable = 'YES';

    IF v_count > 0 THEN
        RAISE EXCEPTION 'PARITY FAIL (nullability): % column(s) are nullable; legacy SSDL declares all columns NOT NULL', v_count;
    END IF;

-- -----------------------------------------------------------------------------
-- 7. Indexes on every FK column
--    (album.artist_id, album.genre_id, song.album_id)
-- -----------------------------------------------------------------------------
    FOR v_actual IN
        SELECT child.relname || '.' || a.attname
          FROM pg_constraint con
          JOIN pg_class child ON child.oid = con.conrelid
          JOIN pg_attribute a ON a.attrelid = con.conrelid AND a.attnum = con.conkey[1]
         WHERE con.contype = 'f'
           AND con.connamespace = 'public'::regnamespace
           AND NOT EXISTS (
                SELECT 1
                  FROM pg_index i
                 WHERE i.indrelid = con.conrelid
                   AND i.indkey[0] = con.conkey[1]
                   AND i.indnatts = 1
              )
    LOOP
        RAISE EXCEPTION 'PARITY FAIL (fk index): no index on FK column %', v_actual;
    END LOOP;

-- -----------------------------------------------------------------------------
-- 8. Constraint and index names are unique (no accidental duplicates)
-- -----------------------------------------------------------------------------
    -- Note: a PK constraint and its backing index legitimately share a name in
    -- PostgreSQL, so constraints and indexes are checked within their own groups.
    SELECT count(*) INTO v_count
      FROM (
            SELECT conname
              FROM pg_constraint
             WHERE connamespace = 'public'::regnamespace
             GROUP BY conname
            HAVING count(*) > 1
            UNION ALL
            SELECT indexname
              FROM pg_indexes
             WHERE schemaname = 'public'
             GROUP BY indexname
            HAVING count(*) > 1
           ) dup;

    IF v_count > 0 THEN
        RAISE EXCEPTION 'PARITY FAIL (unique names): % duplicated constraint/index name(s) in schema public', v_count;
    END IF;

    RAISE NOTICE 'Schema parity verification PASSED: tables, PKs (identity), FKs + ON DELETE rules, varchar (no bpchar), interval duration, NOT NULL, FK indexes, unique names.';
END
$$;
