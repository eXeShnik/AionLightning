CREATE TABLE `account_warehouse_items` (
    `unique_id`     BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `account_id`    INT NOT NULL,
    `item_id`       INT NOT NULL,
    `count`         BIGINT NOT NULL DEFAULT 1,
    `slot`          INT NOT NULL DEFAULT -1,
    `enchant_level` TINYINT NOT NULL DEFAULT 0,
    PRIMARY KEY (`unique_id`),
    INDEX `acc_idx` (`account_id`)
);
