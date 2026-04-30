CREATE TABLE IF NOT EXISTS friend_list (
    player_id  INT         NOT NULL,
    friend_id  INT         NOT NULL,
    note       VARCHAR(100) NOT NULL DEFAULT '',
    PRIMARY KEY (player_id, friend_id),
    FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE CASCADE,
    FOREIGN KEY (friend_id) REFERENCES players(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS block_list (
    player_id  INT         NOT NULL,
    blocked_id INT         NOT NULL,
    reason     VARCHAR(100) NOT NULL DEFAULT '',
    PRIMARY KEY (player_id, blocked_id),
    FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE CASCADE,
    FOREIGN KEY (blocked_id) REFERENCES players(id) ON DELETE CASCADE
);
