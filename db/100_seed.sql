-- =============================================================================
-- MusicStore — Seed data (100_seed.sql)
-- Run once via DbUp (journaled in schemaversions).
--
-- Representative catalog data exercising all tables and FK relationships:
--   3 artists, 3 genres, 4 albums, 8 songs
-- =============================================================================

-- -----------------------------------------------------------------------------
-- Genres
-- -----------------------------------------------------------------------------
INSERT INTO genre (name, description) VALUES
    ('Rock',        'Classic and modern rock music'),
    ('Jazz',        'Improvised music over swing and blues harmonies'),
    ('Electronic',  'Synthesizer-driven dance and ambient music'),
    ('Classical',   'Orchestral and chamber works');

-- -----------------------------------------------------------------------------
-- Artists
-- -----------------------------------------------------------------------------
INSERT INTO artist (name) VALUES
    ('The Midnight Echo'),
    ('Ella Fitzgerald'),
    ('Kraftwerk'),
    ('Ludwig van Beethoven');

-- -----------------------------------------------------------------------------
-- Albums (FK: artist_id, genre_id)
-- -----------------------------------------------------------------------------
INSERT INTO album (artist_id, genre_id, title) VALUES
    (1, 1, 'Neon Horizons'),          -- The Midnight Echo / Rock
    (1, 1, 'Static Dreams'),          -- The Midnight Echo
    (2, 2, 'Ella and Duke'),          -- Ella Fitzgerald
    (4, 4, 'Symphony No. 9');         -- Beethoven

-- -----------------------------------------------------------------------------
-- Songs (durations as interval)
-- -----------------------------------------------------------------------------
INSERT INTO song (album_id, name, duration) VALUES
    (1, 'City Lights',        interval '3m 42s'),
    (1, 'Midnight Drive',     interval '4m 05s'),
    (2, 'Analog Heart',       interval '3m 58s'),
    (2, 'Signal Lost',        interval '5m 12s'),
    (3, 'Dream a Little Dream', interval '3m 04s'),
    (3, 'It Don''t Mean a Thing', interval '3m 11s'),
    (4, 'Ode to Joy',         interval '9m 22s'),
    (4, 'Adagio',             interval '14m 30s');
