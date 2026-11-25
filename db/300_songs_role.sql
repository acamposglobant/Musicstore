-- MIG-21: dedicated PostgreSQL role for the Songs microservice.
-- Both services share the musicstore database; the songs service connects as
-- musicstore_songs and may only touch the song table (least privilege).
-- Idempotent: safe to re-run via DbUp.

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'musicstore_songs') THEN
        CREATE ROLE musicstore_songs LOGIN PASSWORD 'musicstore_songs';
    END IF;
END
$$;

GRANT USAGE ON SCHEMA public TO musicstore_songs;

GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.song TO musicstore_songs;
GRANT SELECT ON TABLE public.album TO musicstore_songs;  -- FK validation on create
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO musicstore_songs;
