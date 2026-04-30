CREATE TABLE IF NOT EXISTS player_quests (
    player_id     INT     NOT NULL,
    quest_id      INT     NOT NULL,
    status        TINYINT NOT NULL DEFAULT 1,
    step          INT     NOT NULL DEFAULT 0,
    complete_count TINYINT NOT NULL DEFAULT 0,
    PRIMARY KEY (player_id, quest_id),
    INDEX idx_player_quests_player (player_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
