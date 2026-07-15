-- Java `challenge_tasks`(task_id, quest_id, owner_id, owner_type, complete_count, complete_time) — per-quest
-- progress toward a legion/town challenge task (see DataHolders/ChallengeData.cs,
-- Services/ChallengeTaskService.cs).
CREATE TABLE IF NOT EXISTS challenge_tasks (
    task_id INT NOT NULL,
    quest_id INT NOT NULL,
    owner_id INT NOT NULL,
    owner_type ENUM('LEGION','TOWN') NOT NULL,
    complete_count INT UNSIGNED NOT NULL DEFAULT 0,
    complete_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (task_id, quest_id, owner_id, owner_type)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Java LegionMember.challengeScore — per-member running score toward the legion's current challenge
-- task's contribution-reward ranking (see ChallengeTaskService.DistributeContributionRewardsAsync),
-- reset to 0 once rewards are distributed for a completed task.
ALTER TABLE legion_members ADD COLUMN challenge_score INT NOT NULL DEFAULT 0;
