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
    // Cumulative EVASION debuff delta (negative = reduced evasion from statdown effects)
    public int EvasionDebuffDelta { get; set; }
    // Cumulative MAXHP delta — positive from statup buffs, negative from statdown debuffs
    public int MaxHpBonusDelta    { get; set; }
    // Cumulative MAGICAL_ATTACK debuff delta (negative = reduced M-attack from statdown effects)
    public int MagicAtkDebuffDelta { get; set; }
    // Cumulative ATTACK_SPEED ADD debuff delta from statdown (positive = slower attacks, adds ms to attack speed)
    public int AtkSpeedDebuffDelta { get; set; }
    // Cumulative MAXMP delta — positive from statup buffs, negative from statdown debuffs
    public int MaxMpBonusDelta     { get; set; }
    // Cumulative BOOST_MAGICAL_SKILL delta — positive from statup buffs, negative from statdown debuffs
    public int MagicBoostDelta     { get; set; }
    // Cumulative HEAL_BOOST delta from statup buffs (adds to BonusHealBoost for heal formula)
    public int HealBoostDelta      { get; set; }
    // Cumulative PHYSICAL_ACCURACY delta (negative from statdown = lower hit rate; positive from statup = higher)
    public int PhysAccDelta        { get; set; }
    // Cumulative MAGICAL_ACCURACY delta (negative from statdown = lower spell hit rate; positive from statup = higher)
    public int MagicAccDelta       { get; set; }
    // Cumulative PARRY delta (negative = reduced parry from statdown; positive = increased from statup)
    public int ParryDelta          { get; set; }
    // Cumulative BLOCK delta (negative = reduced block from statdown; positive = increased from statup)
    public int BlockDelta          { get; set; }
    // Cumulative PHYSICAL_CRITICAL delta (negative = reduced P-crit from statdown; positive from statup)
    public int PhysCritDelta       { get; set; }
    // Cumulative MAGICAL_CRITICAL delta (negative = reduced M-crit from statdown; positive from statup)
    public int MagicCritDelta      { get; set; }
    // Cumulative PHYSICAL_CRITICAL_RESIST delta (positive = more P-crit resist from statup)
    public int PhysCritResistDelta  { get; set; }
    // Cumulative MAGICAL_CRITICAL_RESIST delta (positive = more M-crit resist from statup)
    public int MagicCritResistDelta { get; set; }
    // Cumulative PHYSICAL_CRITICAL_DAMAGE_REDUCE (strike fortitude) delta (positive from statup)
    public int StrikeFortitudeDelta { get; set; }
    // Cumulative MAGICAL_CRITICAL_DAMAGE_REDUCE (spell fortitude) delta (positive from statup)
    public int SpellFortitudeDelta  { get; set; }

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
