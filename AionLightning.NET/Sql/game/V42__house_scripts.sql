CREATE TABLE IF NOT EXISTS house_scripts (
    house_id INT     NOT NULL,
    `index`  TINYINT NOT NULL,
    script   MEDIUMTEXT,
    PRIMARY KEY (house_id, `index`),
    CONSTRAINT fk_house_scripts_house FOREIGN KEY (house_id) REFERENCES houses (id) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
