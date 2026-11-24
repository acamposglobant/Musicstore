# MIG-08 — Schema Parity Verification

**Task:** Verify the PostgreSQL schema (`db/001_create_tables.sql`) against the legacy SQL Server schema (`Legacy/Data/MusicStore.Data.Repository/MusicStoreModel.edmx`, SSDL section).

**Verification script:** [`db/200_verify_schema_parity.sql`](../db/200_verify_schema_parity.sql) — read-only catalog assertions; aborts with an exception on the first failed check. Runs via DbUp (embedded resource, journal table `schemaversions`) and can be re-run standalone with `psql` at any time.

---

## 1. Legacy SSDL → PostgreSQL mapping

| Legacy (SQL Server, `dbo`) | Target (PostgreSQL, `public`) | Parity note |
|---|---|---|
| `Artist(ArtistID int PK IDENTITY, Name nchar(100) NOT NULL)` | `artist(artist_id integer GENERATED ALWAYS AS IDENTITY PK, name varchar(100) NOT NULL)` | `nchar` padding eliminated |
| `Genre(GenreID int PK IDENTITY, Name nchar(50), Description nchar(100))` | `genre(genre_id integer identity PK, name varchar(50), description varchar(100))` | `nchar` padding eliminated |
| `Album(AlbumID int PK IDENTITY, GenreID int FK, ArtistID int FK, Title nchar(100))` | `album(album_id integer identity PK, genre_id, artist_id, title varchar(100))` | FKs carry explicit `ON DELETE` rules |
| `Song(SongID int PK IDENTITY, AlbumID int FK, Name nvarchar(50), Duration time(7))` | `song(song_id integer identity PK, album_id, name varchar(50), duration interval)` | `time(7)` → `interval` (durations beyond 24h representable) |

## 2. Parity checklist

| # | Check | Legacy reference | Verification | Result |
|---|---|---|---|---|
| 1 | Tables `artist`, `genre`, `album`, `song` exist | SSDL `EntitySet` × 4 (`dbo` schema) | `pg_class` / `pg_namespace` | ✅ PASS |
| 2 | PK: single integer column, `GENERATED ALWAYS AS IDENTITY` | SSDL `<Key>` + `IDENTITY` | `pg_index` + `pg_attribute.attidentity` | ✅ PASS |
| 3 | FK `fk_album_artist`: `album.artist_id → artist.artist_id`, `ON DELETE CASCADE` | SSDL `FK_Album_Artist` (Artist 1—\* Album) | `pg_constraint` (`confdeltype = 'c'`) | ✅ PASS |
| 4 | FK `fk_album_genre`: `album.genre_id → genre.genre_id`, `ON DELETE RESTRICT` | SSDL `FK_Album_Genre` (Genre 1—\* Album) | `pg_constraint` (`confdeltype = 'r'`) | ✅ PASS |
| 5 | FK `fk_song_album`: `song.album_id → album.album_id`, `ON DELETE CASCADE` | SSDL `FK_Song_Album` (Album 1—\* Song) | `pg_constraint` (`confdeltype = 'c'`) | ✅ PASS |
| 6 | String columns are `varchar` — **no `bpchar`/`char` padding anywhere** | Legacy debt: `nchar(100)`, `nchar(50)` | `information_schema.columns` (`data_type`) | ✅ PASS |
| 7 | `song.duration` is `interval` | Legacy debt: `time(7)` | `information_schema.columns` | ✅ PASS |
| 8 | All columns `NOT NULL` | SSDL `Nullable="false"` on every property | `information_schema.columns.is_nullable` | ✅ PASS |
| 9 | Index on every FK column (`ix_album_artist_id`, `ix_album_genre_id`, `ix_song_album_id`) | (improvement over legacy — SQL Server auto-indexed FKs only in some cases) | `pg_index` per FK column | ✅ PASS |
| 10 | Constraint/index names unique in schema `public` | — | `pg_constraint` ∪ `pg_indexes` grouped by name | ✅ PASS |

## 3. Deliberate deviations from legacy (documented, not defects)

| Legacy | Target | Reason |
|---|---|---|
| `nchar(100)` / `nchar(50)` (fixed-length, space-padded) | `varchar(100)` / `varchar(50)` | Legacy debt fix mandated by MIG-05; no padding |
| `time(7)` | `interval` | Durations are true time spans; `time` cannot exceed 24h |
| `IDENTITY` | `GENERATED ALWAYS AS IDENTITY` | SQL-standard identity, prevents accidental explicit ID inserts |
| No explicit `ON DELETE` rules | `CASCADE` (album→artist, song→album), `RESTRICT` (album→genre) | Explicit referential actions; genre is shared reference data |
| No FK-column indexes guaranteed | Explicit `ix_*` indexes on all FK columns | Avoid seq-scans on joins/cascades |

## 4. How to run

```bash
# Standalone (any environment with psql):
psql "$DATABASE_URL" -f db/001_create_tables.sql
psql "$DATABASE_URL" -f db/100_seed.sql
psql "$DATABASE_URL" -f db/200_verify_schema_parity.sql   # exits non-zero on parity failure

# Via DbUp (Aspire): the script is an embedded resource of MusicStore.DbMigrations
# and runs once after 001/100; a failure aborts the migration run.
```
