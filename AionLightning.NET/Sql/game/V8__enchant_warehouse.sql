-- Warehouse and enchantment support for player_items
ALTER TABLE `player_items`
    ADD COLUMN `storage_type`  tinyint(1) NOT NULL DEFAULT 0 AFTER `slot`,
    ADD COLUMN `enchant_level` tinyint(1) NOT NULL DEFAULT 0 AFTER `storage_type`,
    ADD INDEX  `player_storage` (`player_id`, `storage_type`);
