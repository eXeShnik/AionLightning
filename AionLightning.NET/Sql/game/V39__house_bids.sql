CREATE TABLE IF NOT EXISTS house_bids (
    player_id INT    NOT NULL,
    house_id  INT    NOT NULL,
    bid       BIGINT NOT NULL,
    bid_time  TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (player_id, house_id, bid),
    KEY house_bids_house_id_idx (house_id),
    CONSTRAINT fk_house_bids_house FOREIGN KEY (house_id) REFERENCES houses (id) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
