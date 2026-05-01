CREATE TABLE IF NOT EXISTS `player_skills` (
    `player_id`   INT      NOT NULL,
    `skill_id`    INT      NOT NULL,
    `skill_level` SMALLINT NOT NULL DEFAULT 1,
    PRIMARY KEY (`player_id`, `skill_id`),
    CONSTRAINT `fk_skills_player` FOREIGN KEY (`player_id`)
        REFERENCES `players`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
