namespace AionLightning.Game.Model;

public abstract class Creature : VisibleObject
{
    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
    public int MaxMp { get; set; }
    public int CurrentMp { get; set; }

    public CreatureState State { get; set; } = CreatureState.Active;
    public VisibleObject? Target { get; set; }

    public int StateValue => (int)State;

    public int HpPercentage => MaxHp > 0 ? (CurrentHp * 100) / MaxHp : 0;
    public bool IsAlreadyDead => CurrentHp <= 0;

    // Tracks when this creature last entered combat — used by RegenService to suppress out-of-combat regen
    public DateTime LastCombatTime { get; set; } = DateTime.MinValue;

    // Tracks the last auto-attack time for cooldown enforcement
    public DateTime LastAttackTime { get; set; } = DateTime.MinValue;

    public float MovementSpeed    { get; set; } = 6.0f;
    public int CurrentAttackSpeed { get; set; } = 1500;

    // Cumulative PHYSICAL_DEFENSE debuff delta (negative = reduced pdef from statdown effects)
    public int PdefDebuffDelta    { get; set; }
    // Cumulative MAGICAL_RESIST debuff delta (negative = reduced magic resist from statdown effects)
    public int MResistDebuffDelta { get; set; }
    // Cumulative PHYSICAL_ATTACK debuff delta (negative = reduced physical attack from statdown effects)
    public int PatkDebuffDelta    { get; set; }

    // Active buff/debuff effects — thread-safe via _effectsLock
    private readonly object              _effectsLock   = new();
    private readonly List<AbnormalState> _activeEffects = new();

    // Aggregated CC flags from all active effects; checked by attack gating
    public AbnormalCcFlags ActiveCcFlags { get; private set; } = AbnormalCcFlags.None;

    public void AddEffect(AbnormalState state)
    {
        lock (_effectsLock)
        {
            _activeEffects.RemoveAll(e => e.SkillId == state.SkillId);
            _activeEffects.Add(state);
            ActiveCcFlags = RebuildCcFlags();
        }
    }

    public void RemoveEffect(int skillId, DateTime expiry)
    {
        lock (_effectsLock)
        {
            _activeEffects.RemoveAll(e => e.SkillId == skillId && e.Expiry == expiry);
            ActiveCcFlags = RebuildCcFlags();
        }
    }

    public void RemoveEffectBySkillId(int skillId)
    {
        lock (_effectsLock)
        {
            _activeEffects.RemoveAll(e => e.SkillId == skillId);
            ActiveCcFlags = RebuildCcFlags();
        }
    }

    public void ClearAllEffects()
    {
        lock (_effectsLock)
        {
            _activeEffects.Clear();
            ActiveCcFlags = AbnormalCcFlags.None;
        }
    }

    public void ClearDebuffs()
    {
        lock (_effectsLock)
        {
            _activeEffects.RemoveAll(e => e.IsDebuff);
            ActiveCcFlags = RebuildCcFlags();
        }
    }

    public void ClearBuffs()
    {
        lock (_effectsLock)
        {
            _activeEffects.RemoveAll(e => !e.IsDebuff);
            ActiveCcFlags = RebuildCcFlags();
        }
    }

    public List<AbnormalState> GetActiveEffects()
    {
        lock (_effectsLock)
            return _activeEffects.Where(e => !e.IsExpired).ToList();
    }

    private AbnormalCcFlags RebuildCcFlags()
    {
        var flags = AbnormalCcFlags.None;
        foreach (var e in _activeEffects)
            if (!e.IsExpired)
                flags |= e.CcFlags;
        return flags;
    }
}
