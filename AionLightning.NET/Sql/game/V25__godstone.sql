-- Godstone socketing support for player_items
DROP PROCEDURE IF EXISTS _mig;
CREATE PROCEDURE _mig() BEGIN
    DECLARE CONTINUE HANDLER FOR 1060 BEGIN END;
    ALTER TABLE `player_items` ADD COLUMN `godstone_item_id` int(11) NOT NULL DEFAULT 0;
END;
CALL _mig();
DROP PROCEDURE IF EXISTS _mig;
