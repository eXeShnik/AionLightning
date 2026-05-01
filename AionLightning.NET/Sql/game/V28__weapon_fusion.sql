ALTER TABLE `player_items`
    ADD COLUMN `fusioned_item_id` int(11) NOT NULL DEFAULT 0 AFTER `skin_item_id`;
