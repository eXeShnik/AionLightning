-- Godstone socketing support for player_items
ALTER TABLE `player_items`
    ADD COLUMN `godstone_item_id` int(11) NOT NULL DEFAULT 0 AFTER `enchant_level`;
