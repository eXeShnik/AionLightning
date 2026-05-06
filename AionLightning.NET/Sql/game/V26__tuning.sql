-- Item tuning (optional socket) support for player_items
DROP PROCEDURE IF EXISTS _mig;
CREATE PROCEDURE _mig() BEGIN
    DECLARE CONTINUE HANDLER FOR 1060 BEGIN END;
    ALTER TABLE `player_items` ADD COLUMN `optional_socket` tinyint(1) NOT NULL DEFAULT -1;
END;
CALL _mig();
DROP PROCEDURE IF EXISTS _mig;
