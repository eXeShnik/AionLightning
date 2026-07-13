CREATE TABLE IF NOT EXISTS houses (
    id            INT            NOT NULL PRIMARY KEY,
    player_id     INT            NOT NULL DEFAULT 0,
    building_id   INT            NOT NULL DEFAULT 0,
    address       INT            NOT NULL DEFAULT 0,
    acquire_time  BIGINT         NOT NULL DEFAULT 0,
    settings      INT            NOT NULL DEFAULT 0,
    status        VARCHAR(16)    NOT NULL DEFAULT 'NoSale',
    fee_paid      TINYINT(1)     NOT NULL DEFAULT 1,
    next_pay      BIGINT         NULL,
    sell_started  BIGINT         NULL,
    sign_notice   VARBINARY(260) NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
