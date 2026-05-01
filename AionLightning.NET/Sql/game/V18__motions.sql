CREATE TABLE IF NOT EXISTS `player_motions` (
    `player_id` INT           NOT NULL,
    `slot`      TINYINT       NOT NULL,
    `motion_id` SMALLINT      NOT NULL,
    PRIMARY KEY (`player_id`, `slot`),
    CONSTRAINT `fk_motions_player` FOREIGN KEY (`player_id`)
        REFERENCES `players`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
