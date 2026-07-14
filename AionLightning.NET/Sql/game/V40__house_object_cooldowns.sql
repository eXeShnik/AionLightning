CREATE TABLE IF NOT EXISTS house_object_cooldowns (
    player_id  INT    NOT NULL,
    object_id  INT    NOT NULL,
    reuse_time BIGINT NOT NULL,
    PRIMARY KEY (player_id, object_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
