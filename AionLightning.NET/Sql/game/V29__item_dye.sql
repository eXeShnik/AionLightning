ALTER TABLE `player_items`
    ADD COLUMN `dye_color` int(11) NOT NULL DEFAULT 0 AFTER `fusioned_item_id`;
