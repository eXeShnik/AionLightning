CREATE TABLE legions (
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(20) NOT NULL UNIQUE,
    level TINYINT NOT NULL DEFAULT 1,
    contribution_points BIGINT NOT NULL DEFAULT 0,
    announcement VARCHAR(2048) NOT NULL DEFAULT '',
    deputy_permission SMALLINT NOT NULL DEFAULT 0,
    centurion_permission SMALLINT NOT NULL DEFAULT 0,
    legionary_permission SMALLINT NOT NULL DEFAULT 0,
    volunteer_permission SMALLINT NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE legion_members (
    player_id INT NOT NULL PRIMARY KEY,
    legion_id INT NOT NULL,
    rank_id TINYINT NOT NULL DEFAULT 3,
    self_intro VARCHAR(255) NOT NULL DEFAULT '',
    nickname VARCHAR(20) NOT NULL DEFAULT '',
    INDEX idx_legion_members_legion (legion_id),
    FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE CASCADE,
    FOREIGN KEY (legion_id) REFERENCES legions(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
