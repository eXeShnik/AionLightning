-- Advanced/regular stigma slots use bit positions 30-52 (Java ItemSlot STIGMA1..6 / ADV_STIGMA1..6),
-- which overflow a 32-bit INT once combined with the IsEquipped bitmask. Widen to BIGINT so equipping
-- stigma stones into slots 3-6 (regular) or any advanced slot persists correctly across relog.
ALTER TABLE `player_items` MODIFY COLUMN `slot` BIGINT NOT NULL DEFAULT -1;
ALTER TABLE `account_warehouse_items` MODIFY COLUMN `slot` BIGINT NOT NULL DEFAULT -1;
