CREATE TABLE broker_items (
    id              INT          NOT NULL AUTO_INCREMENT,
    item_id         INT          NOT NULL DEFAULT 0,
    seller_id       INT          NOT NULL DEFAULT 0,
    seller_name     VARCHAR(50)  NOT NULL DEFAULT '',
    creator_name    VARCHAR(50)  NOT NULL DEFAULT '',
    item_count      BIGINT       NOT NULL DEFAULT 1,
    price           BIGINT       NOT NULL DEFAULT 0,
    race            TINYINT      NOT NULL DEFAULT 0,  -- 0=Elyos, 1=Asmodian
    enchant_level   TINYINT      NOT NULL DEFAULT 0,
    is_settled      TINYINT(1)   NOT NULL DEFAULT 0,
    is_sold         TINYINT(1)   NOT NULL DEFAULT 0,
    is_canceled     TINYINT(1)   NOT NULL DEFAULT 0,
    expire_time     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    settle_time     DATETIME     NULL,
    PRIMARY KEY (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
