CREATE TABLE IF NOT EXISTS player_recipes (
    player_id  INT NOT NULL,
    recipe_id  INT NOT NULL,
    PRIMARY KEY (player_id, recipe_id),
    CONSTRAINT fk_recipe_player FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE CASCADE
);
