-- ============================================================
-- Script de réinitialisation complète de la base de données
-- Mise à jour : Ajout des colonnes Cover URL
-- ============================================================

DROP TABLE IF EXISTS stream CASCADE;
DROP TABLE IF EXISTS track_artist CASCADE;
DROP TABLE IF EXISTS track CASCADE;
DROP TABLE IF EXISTS album CASCADE;
DROP TABLE IF EXISTS artist CASCADE;
DROP TABLE IF EXISTS "user" CASCADE;

-- ============================================================
-- Création des tables
-- ============================================================

CREATE TABLE "user" (
    userId SERIAL PRIMARY KEY,
    userName VARCHAR(255) NOT NULL,
    email VARCHAR(255) UNIQUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE artist (
    artistId SERIAL PRIMARY KEY,
    artistName VARCHAR(255) NOT NULL,
    cover_url TEXT  -- Ajout pour stocker la photo de l'artiste
);

CREATE TABLE album (
    albumId SERIAL PRIMARY KEY,
    albumName VARCHAR(255) NOT NULL,
    cover_url TEXT  -- Ajout pour stocker la pochette d'album
);

CREATE TABLE track (
    trackId SERIAL PRIMARY KEY,
    trackName VARCHAR(255) NOT NULL,
    albumId INT REFERENCES album(albumId) ON DELETE SET NULL,
    cover_url TEXT  -- Ajout pour stocker la cover spécifique au morceau (si différente de l'album)
);

CREATE TABLE track_artist (
    trackId INT REFERENCES track(trackId) ON DELETE CASCADE,
    artistId INT REFERENCES artist(artistId) ON DELETE CASCADE,
    PRIMARY KEY (trackId, artistId)
);

CREATE TABLE stream (
    id SERIAL PRIMARY KEY,
    userId INT NOT NULL REFERENCES "user"(userId) ON DELETE CASCADE,
    trackId INT NOT NULL REFERENCES track(trackId) ON DELETE CASCADE,
    played_at TIMESTAMP NOT NULL,
    listening_time INT 
);

-- ============================================================
-- Création des index
-- ============================================================

CREATE INDEX idx_stream_userId ON stream(userId);
CREATE INDEX idx_stream_trackId ON stream(trackId);
CREATE INDEX idx_stream_played_at ON stream(played_at);
CREATE INDEX idx_stream_user_played ON stream(userId, played_at);
CREATE INDEX idx_stream_user_track_played ON stream(userId, trackId, played_at);

CREATE INDEX idx_track_artist_artistId ON track_artist(artistId);

CREATE INDEX idx_artist_name ON artist(artistName);
CREATE INDEX idx_album_name ON album(albumName);
CREATE INDEX idx_user_name ON "user"(userName);

-- ============================================================
-- Fin du script
-- ============================================================