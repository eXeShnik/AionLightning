-- Item instance storage (inventory + warehouse + equipment)
CREATE TABLE IF NOT EXISTS `player_items` (
  `unique_id` bigint(20) NOT NULL,
  `player_id` int(11) NOT NULL,
  `item_id`   int(11) NOT NULL,
  `count`     bigint(20) NOT NULL DEFAULT '1',
  `slot`      int(11) NOT NULL DEFAULT '-1',
  PRIMARY KEY (`unique_id`),
  KEY `player_id` (`player_id`),
  CONSTRAINT `player_items_ibfk_1` FOREIGN KEY (`player_id`) REFERENCES `players` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Auto-increment source for item unique IDs
CREATE TABLE IF NOT EXISTS `item_unique_id` (
  `id`   bigint(20) NOT NULL AUTO_INCREMENT,
  `stub` tinyint(1) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
