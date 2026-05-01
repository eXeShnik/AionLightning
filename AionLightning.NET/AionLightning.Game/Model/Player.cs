using AionLightning.Game.Model.Group;
using AionLightning.Game.Model.Item;
using LegionModel = AionLightning.Game.Model.Legion.Legion;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Model.Skill;

namespace AionLightning.Game.Model;

public sealed class Player : Creature
{
    public int AccountId { get; init; }
    public Race Race { get; init; }
    public Gender Gender { get; init; }
    public PlayerClass PlayerClass { get; init; }
    public byte Level { get; set; }
    public long Exp { get; set; }
    public int TitleId      { get; set; } = -1;
    public int BonusTitleId { get; set; } = -1;

    // Flight points — not persisted, starts full each login
    public int MaxFp     { get; set; } = 4000;
    public int CurrentFp { get; set; } = 4000;
    public PlayerAppearance Appearance { get; set; } = new();
    public PlayerSkillList  Skills       { get; } = new();
    public PlayerInventory  Inventory    { get; } = new();
    public PlayerInventory  Warehouse    { get; } = new();
    public PlayerQuestList  Quests       { get; } = new();
    public HashSet<int>     KnownRecipes { get; } = new();
    public DateTime CreationDate { get; init; }
    public DateTime? LastOnline { get; set; }
    public DateTime? DeletionDate { get; set; }

    /// <summary>Unix timestamp sent to client; 0 means not pending deletion.</summary>
    public int DeletionTime => DeletionDate.HasValue
        ? (int)new DateTimeOffset(DeletionDate.Value).ToUnixTimeSeconds()
        : 0;

    // Party group — null when not in a group
    public PlayerGroup? Group { get; set; }

    // Legion (guild) — null when not in a legion
    public LegionModel? Legion { get; set; }

    // Bind point (Obelisk) — null means no bind, CM_REVIVE stays in place
    public Position? BindPosition { get; set; }

    // Client display/social settings (from CM_CUSTOM_SETTINGS)
    public int DisplaySettings { get; set; }
    public int DenySettings    { get; set; }

    // Bio note visible to friends in their friend list
    public string Note { get; set; } = string.Empty;

    // UI settings blobs — persisted via IPlayerSettingsDao, sent as SM_UI_SETTINGS on login
    public byte[]? UiSettings   { get; set; }
    public byte[]? Shortcuts    { get; set; }
    public byte[]? HouseBuddies { get; set; }

    // Macros — position (1-48) → macro XML blob sent by client
    public Dictionary<int, string> Macros { get; } = new();

    // Movement state — updated by CM_MOVE, read by SM_MOVE broadcast
    public byte MovementMask { get; set; }
    public float VectorX { get; set; }
    public float VectorY { get; set; }
    public float VectorZ { get; set; }
    public float TargetX2 { get; set; }
    public float TargetY2 { get; set; }
    public float TargetZ2 { get; set; }

    // Abyss rank system
    public long AbyssPoints { get; set; }
    public int  AbyssRank   { get; set; } = 1;

    // Equipped main-hand weapon damage range; both 0 when no weapon is equipped
    public int MainHandMinDmg { get; set; }
    public int MainHandMaxDmg { get; set; }

    // Sum of PHYSICAL_DEFENSE from all equipped items; applied in mitigation formula
    public int PhysicalDefense { get; set; }

    // Motion slots 1-5 (combat animation style); motionId 0 = none
    public Dictionary<byte, short> ActiveMotions { get; } = new();
}
