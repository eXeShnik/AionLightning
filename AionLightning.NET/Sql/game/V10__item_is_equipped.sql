-- Tracks whether an item is currently equipped in an equipment slot.
-- Required because Item.Slot holds the equipment bitmask when equipped and a bag-position
-- when in the bag — without an explicit flag the two cases are indistinguishable.
ALTER TABLE player_items
    ADD COLUMN is_equipped TINYINT(1) NOT NULL DEFAULT 0;

-- Back-fill: items at slot > 0 in inventory storage were written by CM_EQUIP_ITEM
-- and hold the equipment bitmask — mark them as equipped.
UPDATE player_items SET is_equipped = 1 WHERE slot > 0 AND storage_type = 0;
