-- V1 was created without the `level` column; add it retroactively.
ALTER TABLE `players`
    ADD COLUMN IF NOT EXISTS `level` tinyint(3) NOT NULL DEFAULT '1' AFTER `exp`;
