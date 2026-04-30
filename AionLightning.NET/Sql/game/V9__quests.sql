-- Player quest state (active + completed)
CREATE TABLE IF NOT EXISTS `player_quests` (
    `player_id`      int(11)    NOT NULL,
    `quest_id`       int(11)    NOT NULL,
    `status`         tinyint(1) NOT NULL DEFAULT 1,
    `step`           int(11)    NOT NULL DEFAULT 0,
    `complete_count` tinyint(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (`player_id`, `quest_id`),
    KEY `player_id` (`player_id`),
    CONSTRAINT `player_quests_ibfk_1`
        FOREIGN KEY (`player_id`) REFERENCES `players` (`id`)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
