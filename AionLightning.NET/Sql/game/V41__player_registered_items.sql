CREATE TABLE IF NOT EXISTS player_registered_items (
    player_id         INT         NOT NULL,
    item_unique_id    INT         NOT NULL,
    item_id           INT         NOT NULL,
    expire_time       INT         NULL,
    color             INT         NULL,
    color_expires     INT         NOT NULL DEFAULT 0,
    owner_use_count   INT         NOT NULL DEFAULT 0,
    visitor_use_count INT         NOT NULL DEFAULT 0,
    x                 FLOAT       NOT NULL DEFAULT 0,
    y                 FLOAT       NOT NULL DEFAULT 0,
    z                 FLOAT       NOT NULL DEFAULT 0,
    h                 SMALLINT    NULL,
    area              VARCHAR(16) NOT NULL DEFAULT 'NONE',
    floor             TINYINT     NOT NULL DEFAULT 0,
    PRIMARY KEY (player_id, item_unique_id, item_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
