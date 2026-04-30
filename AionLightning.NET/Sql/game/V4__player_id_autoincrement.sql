-- players.id was created without AUTO_INCREMENT; add it so new characters get IDs from MySQL.
ALTER TABLE `players` MODIFY COLUMN `id` int(11) NOT NULL AUTO_INCREMENT;
