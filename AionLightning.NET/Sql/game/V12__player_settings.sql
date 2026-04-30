CREATE TABLE player_settings (
  player_id    INT      NOT NULL,
  settings_type TINYINT  NOT NULL,   -- 0=ui_settings, 1=shortcuts, 2=house_buddies
  settings     MEDIUMBLOB NOT NULL,
  PRIMARY KEY (player_id, settings_type),
  FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
