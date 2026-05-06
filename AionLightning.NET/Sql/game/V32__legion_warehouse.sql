CREATE TABLE IF NOT EXISTS legion_warehouse_items (
    unique_id  BIGINT NOT NULL,
    legion_id  INT    NOT NULL,
    item_id    INT    NOT NULL DEFAULT 0,
    count      BIGINT NOT NULL DEFAULT 1,
    slot       INT    NOT NULL DEFAULT -1,
    enchant_level TINYINT UNSIGNED NOT NULL DEFAULT 0,
    PRIMARY KEY (unique_id),
    INDEX legion_idx (legion_id),
    CONSTRAINT fk_lw_legion FOREIGN KEY (legion_id)
        REFERENCES legions(id) ON DELETE CASCADE
);
