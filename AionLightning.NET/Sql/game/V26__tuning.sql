-- Item tuning (optional socket) support for player_items
ALTER TABLE `player_items`
    ADD COLUMN `optional_socket` tinyint(1) NOT NULL DEFAULT -1 AFTER `godstone_item_id`;
