-- =============================================================================
-- MusicStore — PostgreSQL schema (001_create_tables.sql)
-- Migrated from legacy SQL Server schema (MusicStoreModel.edmx SSDL):
--   Album(ArtistID, GenreID), Artist, Genre, Song(AlbumID)
--
-- Legacy debt fixed here:
--   nchar(100)  -> varchar(100)   (no fixed-length padding)
--   time(7)     -> interval       (durations beyond 24h representable)
--   IDENTITY    -> GENERATED ALWAYS AS IDENTITY
--   FKs carry explicit ON DELETE rules
-- =============================================================================

-- -----------------------------------------------------------------------------
-- artist
-- Legacy: Artist(ArtistID int PK IDENTITY, Name nchar(100) not null)
-- -----------------------------------------------------------------------------
CREATE TABLE artist (
    artist_id      integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name           varchar(100) NOT NULL
);

-- -----------------------------------------------------------------------------
-- genre
-- Legacy: Genre(GenreID int PK IDENTITY, Name nchar(50), Description nchar(100))
-- -----------------------------------------------------------------------------
CREATE TABLE genre (
    genre_id       integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name           varchar(50) NOT NULL,
    description    varchar(100) NOT NULL
);

-- -----------------------------------------------------------------------------
-- album
-- Legacy: Album(AlbumID int PK IDENTITY, GenreID FK, ArtistID FK, Title nchar(100))
-- FK_Album_Artist: Artist 1-* Album
-- FK_Album_Genre:  Genre  1-* Album
-- -----------------------------------------------------------------------------
CREATE TABLE album (
    album_id       integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    genre_id       integer NOT NULL,
    artist_id      integer NOT NULL,
    title          varchar(100) NOT NULL,

    CONSTRAINT fk_album_artist FOREIGN KEY (artist_id)
        REFERENCES artist (artist_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_album_genre FOREIGN KEY (genre_id)
        REFERENCES genre (genre_id)
        ON DELETE RESTRICT
);

CREATE INDEX ix_album_artist_id ON album (artist_id);
CREATE INDEX ix_album_genre_id  ON album (genre_id);

-- -----------------------------------------------------------------------------
-- song
-- Legacy: Song(SongID int PK IDENTITY, AlbumID int FK, Name nvarchar(50), Duration time(7))
-- time(7) -> interval: durations are true time spans (can exceed 24h in theory)
-- -----------------------------------------------------------------------------
CREATE TABLE song (
    song_id        integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    album_id       integer NOT NULL,
    name           varchar(50) NOT NULL,
    duration       interval NOT NULL,

    CONSTRAINT fk_song_album FOREIGN KEY (album_id)
        REFERENCES album (album_id)
        ON DELETE CASCADE
);

CREATE INDEX ix_song_album_id ON song (album_id);
