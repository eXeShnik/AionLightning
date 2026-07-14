using AionLightning.Game.Model;

namespace AionLightning.Game.Model.Town;

/// <summary>
/// Java model.town.Town — a Sanctum/Pandaemonium district's persisted level/points state.
/// note: Java's constructor also re-spawned the town's per-level NPC set (dataholders.TownSpawnsData,
/// SpawnEngine) on creation/level-up; no TownSpawnsData equivalent is ported yet (see migration_plan.md),
/// so this port tracks level/points/persistence only — no NPC availability changes with town level.
/// </summary>
public sealed class Town
{
    /// <summary>Java Town.increasePoints' hardcoded per-level thresholds (index 0 = points needed to
    /// go from level 1 to 2, ... index 3 = level 4 to 5). Level 5 is the maximum (Java never checks
    /// beyond case 4).</summary>
    private static readonly int[] LevelUpThresholds = [1000, 2000, 3000, 4000];

    public int Id { get; }
    public Race Race { get; }
    public int Level { get; private set; }
    public int Points { get; private set; }
    public DateTime LevelUpDate { get; private set; }

    public Town(int id, int level, int points, Race race, DateTime levelUpDate)
    {
        Id = id;
        Level = level;
        Points = points;
        Race = race;
        LevelUpDate = levelUpDate;
    }

    public static Town CreateNew(int id, Race race) => new(id, level: 1, points: 0, race, DateTime.UtcNow);

    /// <summary>Java Town.increasePoints — adds activity points and levels up once the current level's
    /// threshold is met. Returns true when a level-up occurred (caller persists + broadcasts SM_TOWNS_LIST).</summary>
    public bool IncreasePoints(int amount)
    {
        bool leveledUp = false;
        if (Level >= 1 && Level <= LevelUpThresholds.Length && Points + amount >= LevelUpThresholds[Level - 1])
        {
            Level++;
            LevelUpDate = DateTime.UtcNow;
            leveledUp = true;
        }
        Points += amount;
        return leveledUp;
    }
}
