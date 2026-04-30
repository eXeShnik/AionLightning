-- Adds personal warehouse support and enchant level tracking to player items.
-- storage_type: 0 = inventory (default), 1 = personal warehouse
-- enchant_level: 0-15 enchantment progression

ALTER TABLE player_items
  ADD COLUMN storage_type TINYINT UNSIGNED NOT NULL DEFAULT 0 AFTER slot,
  ADD COLUMN enchant_level TINYINT UNSIGNED NOT NULL DEFAULT 0 AFTER storage_type;

CREATE INDEX idx_player_items_player_storage ON player_items (player_id, storage_type);
