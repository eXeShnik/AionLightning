ALTER TABLE players
    ADD COLUMN display_settings SMALLINT NOT NULL DEFAULT 0 AFTER title_id,
    ADD COLUMN deny_settings    SMALLINT NOT NULL DEFAULT 0 AFTER display_settings;
