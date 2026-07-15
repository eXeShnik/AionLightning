-- Java `announcements`(id, announce, faction, type, delay) — scheduled auto server announcements,
-- broadcast on a repeating timer to matching online players. See Services/AnnouncementService.cs.
CREATE TABLE IF NOT EXISTS announcements (
    id INT NOT NULL AUTO_INCREMENT,
    announce TEXT NOT NULL,
    faction ENUM('ALL','ASMODIANS','ELYOS') NOT NULL DEFAULT 'ALL',
    type ENUM('SHOUT','ORANGE','YELLOW','WHITE','SYSTEM') NOT NULL DEFAULT 'SYSTEM',
    delay INT NOT NULL DEFAULT 1800,
    PRIMARY KEY (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Java `veteran_rewards`(id, player, type, item, count, kinah, sender, title, message) — an admin-queued
-- reward-mail table, not account-age login rewards: a minute cron drains pending rows, delivering each as
-- system mail and then deleting it. See Services/VeteranRewardService.cs.
CREATE TABLE IF NOT EXISTS veteran_rewards (
    id INT NOT NULL AUTO_INCREMENT,
    player VARCHAR(255) NOT NULL,
    type INT NOT NULL,
    item INT NOT NULL,
    count INT NOT NULL,
    kinah INT NOT NULL,
    sender VARCHAR(255) NOT NULL,
    title VARCHAR(255) NOT NULL,
    message TEXT,
    PRIMARY KEY (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
