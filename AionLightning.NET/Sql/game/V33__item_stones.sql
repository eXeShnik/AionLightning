CREATE TABLE IF NOT EXISTS item_stones (
    id             BIGINT AUTO_INCREMENT PRIMARY KEY,
    item_unique_id BIGINT NOT NULL,
    item_id        INT    NOT NULL,
    slot           TINYINT NOT NULL,
    INDEX idx_item_stones_item (item_unique_id)
);
