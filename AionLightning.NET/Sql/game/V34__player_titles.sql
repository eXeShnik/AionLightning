CREATE TABLE IF NOT EXISTS player_titles (
    player_id INT NOT NULL,
    title_id  INT NOT NULL,
    PRIMARY KEY (player_id, title_id)
);
