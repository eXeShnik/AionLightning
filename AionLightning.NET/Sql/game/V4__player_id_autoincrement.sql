-- players.id was created without AUTO_INCREMENT; add it so new characters get IDs from MySQL.
SET FOREIGN_KEY_CHECKS=0;
ALTER TABLE `players` MODIFY COLUMN `id` int(11) NOT NULL AUTO_INCREMENT;
SET FOREIGN_KEY_CHECKS=1;
