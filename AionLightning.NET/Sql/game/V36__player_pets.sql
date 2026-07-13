CREATE TABLE IF NOT EXISTS player_pets (
    player_id       INT          NOT NULL,
    pet_id          INT          NOT NULL,
    decoration      INT          NOT NULL DEFAULT 0,
    name            VARCHAR(85)  NOT NULL DEFAULT '',
    birthday        BIGINT       NOT NULL DEFAULT 0,
    despawn_time    BIGINT       NULL,
    expire_time     BIGINT       NULL,
    hungry_level    INT          NOT NULL DEFAULT 0,
    feed_progress   INT          NOT NULL DEFAULT 0,
    reuse_time      BIGINT       NOT NULL DEFAULT 0,
    dopings         VARCHAR(255) NOT NULL DEFAULT '',
    mood_started    BIGINT       NOT NULL DEFAULT 0,
    counter         INT          NOT NULL DEFAULT 0,
    mood_cd_started BIGINT       NOT NULL DEFAULT 0,
    gift_cd_started BIGINT       NOT NULL DEFAULT 0,
    PRIMARY KEY (player_id, pet_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
