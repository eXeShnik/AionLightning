-- Add bind-point columns (set by obelisk/kisk interaction, used by CM_REVIVE)
ALTER TABLE `players`
    ADD COLUMN `bind_x`        float         NULL AFTER `world_id`,
    ADD COLUMN `bind_y`        float         NULL AFTER `bind_x`,
    ADD COLUMN `bind_z`        float         NULL AFTER `bind_y`,
    ADD COLUMN `bind_world_id` int(11)       NULL AFTER `bind_z`;

-- Add client display/deny settings (persisted by CM_CUSTOM_SETTINGS)
ALTER TABLE `players`
    ADD COLUMN `display_settings` smallint NOT NULL DEFAULT 0 AFTER `bonus_title_id`,
    ADD COLUMN `deny_settings`    smallint NOT NULL DEFAULT 0 AFTER `display_settings`;

-- Macro storage (position 1-48 → XML blob, mirrored from Java player_macrosses table)
CREATE TABLE IF NOT EXISTS `player_macrosses` (
    `player_id` int(11)      NOT NULL,
    `order`     int(3)       NOT NULL,
    `macro`     text         NOT NULL,
    UNIQUE KEY `main` (`player_id`, `order`),
    CONSTRAINT `player_macrosses_ibfk_1`
        FOREIGN KEY (`player_id`) REFERENCES `players` (`id`)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
