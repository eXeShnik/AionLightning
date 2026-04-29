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
    public int TitleId { get; set; } = -1;
    public PlayerAppearance Appearance { get; set; } = new();
    public PlayerSkillList  Skills     { get; }      = new();
    public DateTime CreationDate { get; init; }
    public DateTime? LastOnline { get; set; }

    // Movement state — updated by CM_MOVE, read by SM_MOVE broadcast
    public byte MovementMask { get; set; }
    public float VectorX { get; set; }
    public float VectorY { get; set; }
    public float VectorZ { get; set; }
    public float TargetX2 { get; set; }
    public float TargetY2 { get; set; }
    public float TargetZ2 { get; set; }
}
