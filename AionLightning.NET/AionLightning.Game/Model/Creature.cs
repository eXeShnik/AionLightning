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
    // Cumulative BOOST_CASTING_TIME delta (positive from statup = faster casting; negative from statdown = slower)
    public int CastTimeDelta        { get; set; }
    // Cumulative CONCENTRATION delta (negative from statdown = lower magic accuracy; positive from statup)
    public int ConcentrationDelta   { get; set; }
    // Cumulative MAGIC_SKILL_BOOST_RESIST (magic suppression) delta (positive from statup = reduced incoming M-boost)
    public int MagicSuppressionDelta { get; set; }
    // Cumulative PHYSICAL_DEFENSE statup delta (positive from statup buffs)
    public int PdefStatUpDelta  { get; set; }
    // Cumulative MAGICAL_DEFEND delta (negative = debuff, positive = statup buff)
    public int MagicDefDelta    { get; set; }
    // Cumulative PHYSICAL_ATTACK statup delta (positive from statup buffs)
    public int PatkStatUpDelta      { get; set; }
    // Cumulative MAGICAL_ATTACK statup delta (positive from statup buffs)
    public int MagicAtkStatUpDelta  { get; set; }
    // Cumulative EVASION statup delta (positive from statup buffs)
    public int EvasionStatUpDelta   { get; set; }
    // Cumulative MAGICAL_RESIST statup delta (positive from statup buffs)
    public int MResistStatUpDelta   { get; set; }
    // Cumulative ATTACK_SPEED statup delta (negative = faster attacks; subtracts from ms)
    public int AtkSpeedStatUpDelta  { get; set; }

    // Active buff/debuff effects — thread-safe via _effectsLock
    private readonly object              _effectsLock   = new();
    private readonly List<AbnormalState> _activeEffects = new();

    // Aggregated CC flags from all active effects; checked by attack gating
    public AbnormalCcFlags ActiveCcFlags { get; private set; } = AbnormalCcFlags.None;

    public void AddEffect(AbnormalState state)
    {
        lock (_effectsLock)
        {
            foreach (var existing in _activeEffects.Where(e => e.SkillId == state.SkillId))
                ReverseEffectDeltas(existing);
            _activeEffects.RemoveAll(e => e.SkillId == state.SkillId);
            ApplyEffectDeltas(state);
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
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
                if (_activeEffects[i].SkillId == skillId)
                    ReverseEffectDeltas(_activeEffects[i]);
            _activeEffects.RemoveAll(e => e.SkillId == skillId);
            ActiveCcFlags = RebuildCcFlags();
        }
    }

    public void ClearAllEffects()
    {
        lock (_effectsLock)
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
                ReverseEffectDeltas(_activeEffects[i]);
            _activeEffects.Clear();
            ActiveCcFlags = AbnormalCcFlags.None;
        }
    }

    public void ClearDebuffs()
    {
        lock (_effectsLock)
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
                if (_activeEffects[i].IsDebuff) ReverseEffectDeltas(_activeEffects[i]);
            _activeEffects.RemoveAll(e => e.IsDebuff);
            ActiveCcFlags = RebuildCcFlags();
        }
    }

    public void ClearBuffs()
    {
        lock (_effectsLock)
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
                if (!_activeEffects[i].IsDebuff) ReverseEffectDeltas(_activeEffects[i]);
            _activeEffects.RemoveAll(e => !e.IsDebuff);
            ActiveCcFlags = RebuildCcFlags();
        }
    }

    internal void ApplyEffectDeltas(AbnormalState e)
    {
        int v;
        if ((v = e.PdefDelta)               != 0) PdefDebuffDelta       += v;
        if ((v = e.MResistDelta)            != 0) MResistDebuffDelta    += v;
        if ((v = e.PatkDelta)               != 0) PatkDebuffDelta       += v;
        if ((v = e.EvasionDelta)            != 0) EvasionDebuffDelta    += v;
        if ((v = e.MaxHpDelta)              != 0)
        {
            MaxHpBonusDelta += v;
            if (v < 0) { int cap = Math.Max(1, MaxHp + MaxHpBonusDelta); if (CurrentHp > cap) CurrentHp = cap; }
        }
        if ((v = e.MagicAtkDelta)           != 0) MagicAtkDebuffDelta   += v;
        if ((v = e.AtkSpeedDelta)           != 0) AtkSpeedDebuffDelta   += v;
        if ((v = e.MaxMpDelta)              != 0)
        {
            MaxMpBonusDelta += v;
            if (v < 0) { int cap = Math.Max(1, MaxMp + MaxMpBonusDelta); if (CurrentMp > cap) CurrentMp = cap; }
        }
        if ((v = e.MagicBoostDeltaVal)      != 0) MagicBoostDelta       += v;
        if ((v = e.HealBoostDeltaVal)       != 0) HealBoostDelta        += v;
        if ((v = e.PhysAccDeltaVal)         != 0) PhysAccDelta          += v;
        if ((v = e.MagicAccDeltaVal)        != 0) MagicAccDelta         += v;
        if ((v = e.ParryDeltaVal)           != 0) ParryDelta            += v;
        if ((v = e.BlockDeltaVal)           != 0) BlockDelta            += v;
        if ((v = e.PhysCritDeltaVal)        != 0) PhysCritDelta         += v;
        if ((v = e.MagicCritDeltaVal)       != 0) MagicCritDelta        += v;
        if ((v = e.PhysCritResistDeltaVal)  != 0) PhysCritResistDelta   += v;
        if ((v = e.MagicCritResistDeltaVal) != 0) MagicCritResistDelta  += v;
        if ((v = e.StrikeFortitudeDeltaVal) != 0) StrikeFortitudeDelta  += v;
        if ((v = e.SpellFortitudeDeltaVal)  != 0) SpellFortitudeDelta   += v;
        if ((v = e.CastTimeDeltaVal)        != 0) CastTimeDelta         += v;
        if ((v = e.ConcentrationDeltaVal)   != 0) ConcentrationDelta    += v;
        if ((v = e.MagicSuppressionDeltaVal)!= 0) MagicSuppressionDelta += v;
        if ((v = e.PdefStatUpDeltaVal)      != 0) PdefStatUpDelta       += v;
        if ((v = e.MagicDefDeltaVal)        != 0) MagicDefDelta         += v;
        if ((v = e.PatkStatUpDeltaVal)      != 0) PatkStatUpDelta       += v;
        if ((v = e.MagicAtkStatUpDeltaVal)  != 0) MagicAtkStatUpDelta   += v;
        if ((v = e.EvasionStatUpDeltaVal)   != 0) EvasionStatUpDelta    += v;
        if ((v = e.MResistStatUpDeltaVal)   != 0) MResistStatUpDelta    += v;
        if ((v = e.AtkSpeedStatUpDeltaVal)  != 0) AtkSpeedStatUpDelta   += v;
    }

    internal void ReverseEffectDeltas(AbnormalState e)
    {
        int v;
        if ((v = e.PdefDelta)               != 0) PdefDebuffDelta       -= v;
        if ((v = e.MResistDelta)            != 0) MResistDebuffDelta    -= v;
        if ((v = e.PatkDelta)               != 0) PatkDebuffDelta       -= v;
        if ((v = e.EvasionDelta)            != 0) EvasionDebuffDelta    -= v;
        if ((v = e.MagicAtkDelta)           != 0) MagicAtkDebuffDelta   -= v;
        if ((v = e.AtkSpeedDelta)           != 0) AtkSpeedDebuffDelta   -= v;
        if ((v = e.MaxHpDelta)              != 0) MaxHpBonusDelta       -= v;
        if ((v = e.MaxMpDelta)              != 0) MaxMpBonusDelta       -= v;
        if ((v = e.MagicBoostDeltaVal)      != 0) MagicBoostDelta       -= v;
        if ((v = e.HealBoostDeltaVal)       != 0) HealBoostDelta        -= v;
        if ((v = e.PhysAccDeltaVal)         != 0) PhysAccDelta          -= v;
        if ((v = e.MagicAccDeltaVal)        != 0) MagicAccDelta         -= v;
        if ((v = e.ParryDeltaVal)           != 0) ParryDelta            -= v;
        if ((v = e.BlockDeltaVal)           != 0) BlockDelta            -= v;
        if ((v = e.PhysCritDeltaVal)        != 0) PhysCritDelta         -= v;
        if ((v = e.MagicCritDeltaVal)       != 0) MagicCritDelta        -= v;
        if ((v = e.PhysCritResistDeltaVal)  != 0) PhysCritResistDelta   -= v;
        if ((v = e.MagicCritResistDeltaVal) != 0) MagicCritResistDelta  -= v;
        if ((v = e.StrikeFortitudeDeltaVal) != 0) StrikeFortitudeDelta  -= v;
        if ((v = e.SpellFortitudeDeltaVal)  != 0) SpellFortitudeDelta   -= v;
        if ((v = e.CastTimeDeltaVal)        != 0) CastTimeDelta         -= v;
        if ((v = e.ConcentrationDeltaVal)   != 0) ConcentrationDelta    -= v;
        if ((v = e.MagicSuppressionDeltaVal)!= 0) MagicSuppressionDelta -= v;
        if ((v = e.PdefStatUpDeltaVal)      != 0) PdefStatUpDelta       -= v;
        if ((v = e.MagicDefDeltaVal)        != 0) MagicDefDelta         -= v;
        if ((v = e.PatkStatUpDeltaVal)      != 0) PatkStatUpDelta       -= v;
        if ((v = e.MagicAtkStatUpDeltaVal)  != 0) MagicAtkStatUpDelta   -= v;
        if ((v = e.EvasionStatUpDeltaVal)   != 0) EvasionStatUpDelta    -= v;
        if ((v = e.MResistStatUpDeltaVal)   != 0) MResistStatUpDelta    -= v;
        if ((v = e.AtkSpeedStatUpDeltaVal)  != 0) AtkSpeedStatUpDelta   -= v;
        if (e.MovSpeedPct    != 0) MovementSpeed      = e.PreDebuffSpeed;
        if (e.AttackSpeedPct != 0) CurrentAttackSpeed = e.PreDebuffAtkSpeed;
        if (e.SpeedStatUpPct != 0) MovementSpeed      = e.PreBuffMovSpeed;
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
