using AionLightning.Game.Model.Templates.Tribe;
using AionLightning.Game.Services;

namespace AionLightning.Game.Model;

public abstract class Creature : VisibleObject
{
    /// <summary>Java <c>Creature.getTribe()</c>. Overridden by <see cref="Npc"/> (its template's tribe)
    /// and <see cref="Player"/> (PC/PC_DARK by race); the base default mirrors the Java base-class
    /// fallback (which is never actually reached for the two live subtypes).</summary>
    public virtual TribeClass Tribe { get; } = TribeClass.General;

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
    // Cumulative HEAL_SKILL_DEBOOST delta (negative = receive less healing, positive = receive more; percent)
    public int HealReceivedPct      { get; set; }
    // Cumulative DR_BOOST delta from statup buffs (positive = % more item drop chance per kill)
    public int DRBoostDelta         { get; set; }
    // Cumulative ABNORMAL_RESISTANCE_ALL delta from statup buffs (0–10000 scale; Random.Next(10001) < value = CC resisted)
    public int CcResistAll          { get; set; }
    // M338: per-CC-type resist accumulators (Java 0–1000 scale; Random.Next(1001) < value = specific CC resisted).
    public int StunResist        { get; set; }
    public int StumbleResist     { get; set; }
    public int StaggerResist     { get; set; }
    public int SpinResist        { get; set; }
    public int SleepResist       { get; set; }
    public int FearResist        { get; set; }
    public int OpenAerialResist  { get; set; }
    public int RootResist        { get; set; }
    public int SnareResist       { get; set; }
    public int FireResist        { get; set; }
    public int WaterResist       { get; set; }
    public int WindResist        { get; set; }
    public int EarthResist       { get; set; }
    public int PvpAtkRatio       { get; set; }
    public int PvpDefRatio       { get; set; }

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
        List<AbnormalState> removed;
        lock (_effectsLock)
        {
            removed = _activeEffects.Where(e => e.SkillId == skillId && e.Expiry == expiry).ToList();
            _activeEffects.RemoveAll(e => e.SkillId == skillId && e.Expiry == expiry);
            ActiveCcFlags = RebuildCcFlags();
        }
        foreach (var e in removed) EffectTickScheduler.Instance?.CancelForEffect(e);
    }

    public void RemoveEffectBySkillId(int skillId)
    {
        var removed = new List<AbnormalState>();
        lock (_effectsLock)
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
                if (_activeEffects[i].SkillId == skillId)
                {
                    ReverseEffectDeltas(_activeEffects[i]);
                    removed.Add(_activeEffects[i]);
                }
            _activeEffects.RemoveAll(e => e.SkillId == skillId);
            ActiveCcFlags = RebuildCcFlags();
        }
        foreach (var e in removed) EffectTickScheduler.Instance?.CancelForEffect(e);
    }

    // M289: locate first non-expired effect by stack group name (e.g. "SYSTEM_SKILL_SIGNET1")
    public AbnormalState? GetEffectByStack(string stackName)
    {
        lock (_effectsLock)
            return _activeEffects.FirstOrDefault(e =>
                !e.IsExpired && string.Equals(e.StackName, stackName, StringComparison.Ordinal));
    }

    // M289: remove all effects matching a stack group name and reverse their deltas (idempotent)
    public void RemoveEffectByStack(string stackName)
    {
        var removed = new List<AbnormalState>();
        lock (_effectsLock)
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
                if (string.Equals(_activeEffects[i].StackName, stackName, StringComparison.Ordinal))
                {
                    ReverseEffectDeltas(_activeEffects[i]);
                    removed.Add(_activeEffects[i]);
                }
            _activeEffects.RemoveAll(e =>
                string.Equals(e.StackName, stackName, StringComparison.Ordinal));
            ActiveCcFlags = RebuildCcFlags();
        }
        foreach (var e in removed) EffectTickScheduler.Instance?.CancelForEffect(e);
    }

    public void ClearAllEffects()
    {
        List<AbnormalState> removed;
        lock (_effectsLock)
        {
            removed = new List<AbnormalState>(_activeEffects);
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
                ReverseEffectDeltas(_activeEffects[i]);
            _activeEffects.Clear();
            ActiveCcFlags = AbnormalCcFlags.None;
        }
        foreach (var e in removed) EffectTickScheduler.Instance?.CancelForEffect(e);
    }

    public void ClearDebuffs()
    {
        List<AbnormalState> removed;
        lock (_effectsLock)
        {
            removed = _activeEffects.Where(e => e.IsDebuff).ToList();
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
                if (_activeEffects[i].IsDebuff) ReverseEffectDeltas(_activeEffects[i]);
            _activeEffects.RemoveAll(e => e.IsDebuff);
            ActiveCcFlags = RebuildCcFlags();
        }
        foreach (var e in removed) EffectTickScheduler.Instance?.CancelForEffect(e);
    }

    // M380: Remove up to maxCount debuffs whose DispelCategory matches dispelCat and whose ReqDispelLevel <= dispelLevel.
    // dispelCat "ALL" matches DEBUFF_PHYSICAL, DEBUFF_MENTAL, and ALL-category debuffs.
    // Specific categories (DEBUFF_PHYSICAL / DEBUFF_MENTAL) also match debuffs tagged ALL.
    // Permanent effects (Expiry == DateTime.MaxValue) are never dispellable.
    public void ClearDebuffsByCategory(string dispelCat, int maxCount, int dispelLevel)
    {
        var removed = new List<AbnormalState>();
        lock (_effectsLock)
        {
            int removedCount = 0;
            for (int i = _activeEffects.Count - 1; i >= 0 && removedCount < maxCount; i--)
            {
                var e = _activeEffects[i];
                if (!e.IsDebuff) continue;
                if (e.Expiry == DateTime.MaxValue) continue;
                if (e.ReqDispelLevel > dispelLevel) continue;
                bool catMatch = dispelCat == "ALL"
                    ? e.DispelCategory is "ALL" or "DEBUFF_PHYSICAL" or "DEBUFF_MENTAL"
                    : e.DispelCategory == "ALL" || e.DispelCategory == dispelCat;
                if (!catMatch) continue;
                ReverseEffectDeltas(e);
                _activeEffects.RemoveAt(i);
                removed.Add(e);
                removedCount++;
            }
            ActiveCcFlags = RebuildCcFlags();
        }
        foreach (var e in removed) EffectTickScheduler.Instance?.CancelForEffect(e);
    }

    public void ClearBuffs()
    {
        List<AbnormalState> removed;
        lock (_effectsLock)
        {
            removed = _activeEffects.Where(e => !e.IsDebuff && !e.IsSanctuary).ToList();
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
                if (!_activeEffects[i].IsDebuff && !_activeEffects[i].IsSanctuary) ReverseEffectDeltas(_activeEffects[i]);
            _activeEffects.RemoveAll(e => !e.IsDebuff && !e.IsSanctuary);
            ActiveCcFlags = RebuildCcFlags();
        }
        foreach (var e in removed) EffectTickScheduler.Instance?.CancelForEffect(e);
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
        if ((v = e.HealReceivedPctDelta)    != 0) HealReceivedPct       += v;
        if ((v = e.DRBoostDeltaVal)         != 0) DRBoostDelta          += v;
        if ((v = e.CcResistAllDeltaVal)     != 0) CcResistAll           += v;
        if ((v = e.StunResistDelta)         != 0) StunResist            += v;
        if ((v = e.StumbleResistDelta)      != 0) StumbleResist         += v;
        if ((v = e.StaggerResistDelta)      != 0) StaggerResist         += v;
        if ((v = e.SpinResistDelta)         != 0) SpinResist            += v;
        if ((v = e.SleepResistDelta)        != 0) SleepResist           += v;
        if ((v = e.FearResistDelta)         != 0) FearResist            += v;
        if ((v = e.OpenAerialResistDelta)   != 0) OpenAerialResist      += v;
        if ((v = e.RootResistDelta)         != 0) RootResist            += v;
        if ((v = e.SnareResistDelta)        != 0) SnareResist           += v;
        if ((v = e.FireResistDelta)         != 0) FireResist            += v;
        if ((v = e.WaterResistDelta)        != 0) WaterResist           += v;
        if ((v = e.WindResistDelta)         != 0) WindResist            += v;
        if ((v = e.EarthResistDelta)        != 0) EarthResist           += v;
        if ((v = e.PvpAtkRatioDelta)        != 0) PvpAtkRatio           += v;
        if ((v = e.PvpDefRatioDelta)        != 0) PvpDefRatio           += v;
        if (e.RegenHpPctDeltaVal  != 0 && this is Player regenHpApply)  regenHpApply.BonusRegenHpPct  += e.RegenHpPctDeltaVal;
        if (e.RegenMpPctDeltaVal  != 0 && this is Player regenMpApply)  regenMpApply.BonusRegenMpPct  += e.RegenMpPctDeltaVal;
        if (e.RegenFpPctDeltaVal  != 0 && this is Player regenFpApply)  regenFpApply.BonusRegenFpPct  += e.RegenFpPctDeltaVal;
        if (e.RegenHpAddDeltaVal  != 0 && this is Player regenHpFlAt)   regenHpFlAt.BonusRegenHpFlat  += e.RegenHpAddDeltaVal;
        if (e.RegenMpAddDeltaVal  != 0 && this is Player regenMpFlAt)   regenMpFlAt.BonusRegenMpFlat  += e.RegenMpAddDeltaVal;
        if (e.APBoostDeltaVal       != 0 && this is Player apBoostApply)   apBoostApply.APBoostDelta    += e.APBoostDeltaVal;
        if (e.BoostHatePctDeltaVal  != 0 && this is Player boostHateApply) boostHateApply.BoostHatePct  += e.BoostHatePctDeltaVal;
        if (e.FlyTimePctDeltaVal    != 0 && this is Player flyTimeApply)
        {
            flyTimeApply.BonusFlyTimePct += e.FlyTimePctDeltaVal;
            int fpCap = Math.Max(1, flyTimeApply.EffectiveMaxFp);
            if (flyTimeApply.CurrentFp > fpCap) flyTimeApply.CurrentFp = fpCap;
        }
        if (e.FlyTimeAddDeltaVal    != 0 && this is Player flyTimeAddApply)
        {
            flyTimeAddApply.BonusFlyTimeFlat += e.FlyTimeAddDeltaVal;
            int fpCapAdd = Math.Max(1, flyTimeAddApply.EffectiveMaxFp);
            if (flyTimeAddApply.CurrentFp > fpCapAdd) flyTimeAddApply.CurrentFp = fpCapAdd;
        }
        if (e.HealSkillBoostPct      != 0 && this is Player healBoostApply) healBoostApply.BonusHealSkillBoostPct   += e.HealSkillBoostPct;
        if (e.BoostSkillCostPct      != 0 && this is Player costBoostApply) costBoostApply.BonusSkillCostBoostPct   += e.BoostSkillCostPct;
        if (e.HuntingXpBoostPct      != 0 && this is Player xpApply)        xpApply.BonusHuntingXpPct            += e.HuntingXpBoostPct;
        if (e.GroupHuntingXpBoostPct != 0 && this is Player grpXpApply)     grpXpApply.BonusGroupHuntingXpPct    += e.GroupHuntingXpBoostPct;
        if (e.BoostDropRateDeltaVal  != 0 && this is Player dropApply)      dropApply.BonusDropRatePct            += e.BoostDropRateDeltaVal;
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
        if ((v = e.HealReceivedPctDelta)    != 0) HealReceivedPct       -= v;
        if ((v = e.DRBoostDeltaVal)         != 0) DRBoostDelta          -= v;
        if ((v = e.CcResistAllDeltaVal)     != 0) CcResistAll           -= v;
        if ((v = e.StunResistDelta)         != 0) StunResist            -= v;
        if ((v = e.StumbleResistDelta)      != 0) StumbleResist         -= v;
        if ((v = e.StaggerResistDelta)      != 0) StaggerResist         -= v;
        if ((v = e.SpinResistDelta)         != 0) SpinResist            -= v;
        if ((v = e.SleepResistDelta)        != 0) SleepResist           -= v;
        if ((v = e.FearResistDelta)         != 0) FearResist            -= v;
        if ((v = e.OpenAerialResistDelta)   != 0) OpenAerialResist      -= v;
        if ((v = e.RootResistDelta)         != 0) RootResist            -= v;
        if ((v = e.SnareResistDelta)        != 0) SnareResist           -= v;
        if ((v = e.FireResistDelta)         != 0) FireResist            -= v;
        if ((v = e.WaterResistDelta)        != 0) WaterResist           -= v;
        if ((v = e.WindResistDelta)         != 0) WindResist            -= v;
        if ((v = e.EarthResistDelta)        != 0) EarthResist           -= v;
        if ((v = e.PvpAtkRatioDelta)        != 0) PvpAtkRatio           -= v;
        if ((v = e.PvpDefRatioDelta)        != 0) PvpDefRatio           -= v;
        if (e.MovSpeedPct    != 0) MovementSpeed      = e.PreDebuffSpeed;
        if (e.AttackSpeedPct != 0) CurrentAttackSpeed = e.PreDebuffAtkSpeed;
        if (e.SpeedStatUpPct != 0) MovementSpeed      = e.PreBuffMovSpeed;
        if (e.RegenHpPctDeltaVal  != 0 && this is Player regenHpRev)  regenHpRev.BonusRegenHpPct  -= e.RegenHpPctDeltaVal;
        if (e.RegenMpPctDeltaVal  != 0 && this is Player regenMpRev)  regenMpRev.BonusRegenMpPct  -= e.RegenMpPctDeltaVal;
        if (e.RegenFpPctDeltaVal  != 0 && this is Player regenFpRev)  regenFpRev.BonusRegenFpPct  -= e.RegenFpPctDeltaVal;
        if (e.RegenHpAddDeltaVal  != 0 && this is Player regenHpFlRv) regenHpFlRv.BonusRegenHpFlat -= e.RegenHpAddDeltaVal;
        if (e.RegenMpAddDeltaVal  != 0 && this is Player regenMpFlRv) regenMpFlRv.BonusRegenMpFlat -= e.RegenMpAddDeltaVal;
        if (e.APBoostDeltaVal       != 0 && this is Player apBoostRev)   apBoostRev.APBoostDelta   -= e.APBoostDeltaVal;
        if (e.BoostHatePctDeltaVal  != 0 && this is Player boostHateRev) boostHateRev.BoostHatePct -= e.BoostHatePctDeltaVal;
        if (e.FlyTimePctDeltaVal    != 0 && this is Player flyTimeRev)
        {
            flyTimeRev.BonusFlyTimePct -= e.FlyTimePctDeltaVal;
            int fpCap = Math.Max(1, flyTimeRev.EffectiveMaxFp);
            if (flyTimeRev.CurrentFp > fpCap) flyTimeRev.CurrentFp = fpCap;
        }
        if (e.FlyTimeAddDeltaVal    != 0 && this is Player flyTimeAddRev)
        {
            flyTimeAddRev.BonusFlyTimeFlat -= e.FlyTimeAddDeltaVal;
            int fpCapAddRev = Math.Max(1, flyTimeAddRev.EffectiveMaxFp);
            if (flyTimeAddRev.CurrentFp > fpCapAddRev) flyTimeAddRev.CurrentFp = fpCapAddRev;
        }
        if (e.HealSkillBoostPct      != 0 && this is Player healBoostRev) healBoostRev.BonusHealSkillBoostPct   -= e.HealSkillBoostPct;
        if (e.BoostSkillCostPct      != 0 && this is Player costBoostRev) costBoostRev.BonusSkillCostBoostPct   -= e.BoostSkillCostPct;
        if (e.HuntingXpBoostPct      != 0 && this is Player xpRev)        xpRev.BonusHuntingXpPct            -= e.HuntingXpBoostPct;
        if (e.GroupHuntingXpBoostPct != 0 && this is Player grpXpRev)     grpXpRev.BonusGroupHuntingXpPct    -= e.GroupHuntingXpBoostPct;
        if (e.BoostDropRateDeltaVal  != 0 && this is Player dropRev)      dropRev.BonusDropRatePct            -= e.BoostDropRateDeltaVal;
    }

    /// <summary>M334: consume one onetimecrit charge; returns (flatBoost, pctBoost) or (0, 0) when no active buff.</summary>
    public (int FlatBoost, int PctBoost) ConsumeOnetimeCritCharge()
    {
        lock (_effectsLock)
        {
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                var e = _activeEffects[i];
                if (e.IsExpired || e.OnetimeCritCountRemaining <= 0) continue;
                e.OnetimeCritCountRemaining--;
                int flat = e.OnetimeCritBoostFlat;
                int pct  = e.OnetimeCritBoostPct;
                if (e.OnetimeCritCountRemaining == 0) _activeEffects.RemoveAt(i);
                return (flat, pct);
            }
            return (0, 0);
        }
    }

    /// <summary>M334: consume one onetimeatk charge matching the skill's physical/magical type; returns boost% or 0.</summary>
    public int ConsumeOnetimeAtkCharge(bool isPhysical)
    {
        lock (_effectsLock)
        {
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                var e = _activeEffects[i];
                if (e.IsExpired || e.OnetimeAtkCountRemaining <= 0) continue;
                if (e.OnetimeAtkBoostIsPhysical != isPhysical) continue;
                e.OnetimeAtkCountRemaining--;
                int pct = e.OnetimeAtkBoostPct;
                if (e.OnetimeAtkCountRemaining == 0) _activeEffects.RemoveAt(i);
                return pct;
            }
            return 0;
        }
    }

    // M360: consume one alwaysresist charge when an incoming magical spell would land.
    // Returns true if the spell is auto-resisted (caller must skip damage). Removes buff when last charge consumed.
    public bool TryConsumeResistCharge()
    {
        int shieldSkillId = -1;
        bool consumed = false;
        lock (_effectsLock)
        {
            var effect = _activeEffects.FirstOrDefault(e => !e.IsExpired && e.AlwaysResistCountRemaining > 0);
            if (effect is null) return false;
            consumed = true;
            effect.AlwaysResistCountRemaining--;
            if (effect.AlwaysResistCountRemaining <= 0)
                shieldSkillId = effect.SkillId;
        }
        if (shieldSkillId >= 0)
            RemoveEffectBySkillId(shieldSkillId);
        return consumed;
    }

    // M359: absorb incoming damage through an active shield buff. Returns post-absorption damage.
    // Absorbs up to ShieldHitValue (or ShieldHitValue% when IsShieldPercent) per hit, capped by remaining pool.
    // Removes the shield buff when pool reaches 0.
    public int TryAbsorbShield(int damage) => TryAbsorbShield(damage, out _);

    public int TryAbsorbShield(int damage, out int absorbingSkillId)
    {
        absorbingSkillId = -1;
        int removeSkillId = -1;
        int absorbed = 0;
        lock (_effectsLock)
        {
            var shield = _activeEffects.FirstOrDefault(e => !e.IsExpired && e.ShieldPoolRemaining > 0);
            if (shield is null) return damage;

            int cap = shield.IsShieldPercent
                ? damage * shield.ShieldHitValue / 100
                : Math.Min(shield.ShieldHitValue, damage);
            absorbed = Math.Min(cap, shield.ShieldPoolRemaining);
            if (absorbed <= 0) return damage;

            absorbingSkillId = shield.SkillId;
            shield.ShieldPoolRemaining -= absorbed;
            if (shield.ShieldPoolRemaining <= 0)
                removeSkillId = shield.SkillId;
        }
        if (removeSkillId >= 0)
            RemoveEffectBySkillId(removeSkillId);
        return Math.Max(0, damage - absorbed);
    }

    /// <summary>M366: intercept incoming damage with an active mpshield buff; the absorbed amount is drained from the
    /// target's MP instead of HP. Returns remaining damage after absorption and sets absorbingSkillId.</summary>
    public int TryAbsorbMpShield(int damage, out int absorbingSkillId)
    {
        absorbingSkillId = -1;
        int removeSkillId = -1;
        int absorbed = 0;
        lock (_effectsLock)
        {
            var shield = _activeEffects.FirstOrDefault(e => !e.IsExpired && e.MpShieldPoolRemaining > 0);
            if (shield is null) return damage;

            int cap = shield.IsMpShieldPercent
                ? damage * shield.MpShieldHitValue / 100
                : Math.Min(shield.MpShieldHitValue, damage);
            absorbed = Math.Min(cap, shield.MpShieldPoolRemaining);
            if (absorbed <= 0) return damage;

            absorbingSkillId = shield.SkillId;
            shield.MpShieldPoolRemaining -= absorbed;
            if (shield.MpShieldPoolRemaining <= 0)
                removeSkillId = shield.SkillId;
        }
        if (removeSkillId >= 0)
            RemoveEffectBySkillId(removeSkillId);
        if (this is Player mpDrainPlayer)
            mpDrainPlayer.CurrentMp = Math.Max(0, mpDrainPlayer.CurrentMp - absorbed);
        return Math.Max(0, damage - absorbed);
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
