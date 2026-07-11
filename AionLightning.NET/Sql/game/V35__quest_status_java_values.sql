-- Align player_quests.status with the Java/client QuestStatus values.
-- Old C# enum: START=1, REWARD=2, COMPLETE=3.
-- Java/client:  NONE=0, START=3, REWARD=4, COMPLETE=5, LOCKED=6.
-- Single CASE evaluates against the pre-update value, so old COMPLETE(3) maps
-- to 5 before new START(3) rows can exist.
UPDATE player_quests SET status = CASE status
    WHEN 1 THEN 3
    WHEN 2 THEN 4
    WHEN 3 THEN 5
    ELSE status
END;

ALTER TABLE player_quests MODIFY COLUMN `status` tinyint(1) NOT NULL DEFAULT 3;
