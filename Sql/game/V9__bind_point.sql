ALTER TABLE players
    ADD COLUMN bind_x        FLOAT NULL AFTER z,
    ADD COLUMN bind_y        FLOAT NULL AFTER bind_x,
    ADD COLUMN bind_z        FLOAT NULL AFTER bind_y,
    ADD COLUMN bind_world_id INT   NULL AFTER bind_z;
