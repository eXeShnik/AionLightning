namespace AionLightning.Game.Model;

/// <summary>Tracks a single active buff or debuff on a creature.</summary>
public sealed class AbnormalState
{
    public int             SkillId    { get; init; }
    public int             SkillLevel { get; init; }
    public int             EffectorId { get; init; }
    public DateTime        Expiry     { get; init; }
    public AbnormalCcFlags CcFlags    { get; init; } = AbnormalCcFlags.None;

    public bool IsExpired   => DateTime.UtcNow >= Expiry;
    public int  RemainingMs => IsExpired ? 0 : (int)Math.Min((Expiry - DateTime.UtcNow).TotalMilliseconds, int.MaxValue);
}
