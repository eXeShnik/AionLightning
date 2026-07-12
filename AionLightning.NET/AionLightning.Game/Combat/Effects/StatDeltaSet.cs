namespace AionLightning.Game.Combat.Effects;

/// <summary>
/// S4a: pure stat-delta output of <see cref="StatEffectCalculator.Compute"/> — mirrors the ~50 locals
/// that CM_CASTSPELL's buff-application block used to compute inline before folding them into a single
/// <see cref="AionLightning.Game.Model.AbnormalState"/>. Fields are mutable so the calculator can accumulate
/// (statup + statdown self-nerf + flat ADD) exactly like the original locals did.
/// </summary>
public sealed class StatDeltaSet
{
    public int SpeedStatUpPct;
    public int MaxHpDelta;
    public int MaxMpDelta;
    public int MagicBoostDeltaVal;
    public int HealBoostDeltaVal;
    public int PhysAccDeltaVal;
    public int MagicAccDeltaVal;
    public int ParryDeltaVal;
    public int BlockDeltaVal;
    public int PhysCritDeltaVal;
    public int MagicCritDeltaVal;
    public int PhysCritResistDeltaVal;
    public int MagicCritResistDeltaVal;
    public int StrikeFortitudeDeltaVal;
    public int SpellFortitudeDeltaVal;
    public int CastTimeDeltaVal;
    public int ConcentrationDeltaVal;
    public int MagicSuppressionDeltaVal;
    public int PdefStatUpDeltaVal;
    public int MagicDefDeltaVal;
    public int PatkStatUpDeltaVal;
    public int MagicAtkStatUpDeltaVal;
    public int EvasionStatUpDeltaVal;
    public int MResistStatUpDeltaVal;
    public int AtkSpeedStatUpDeltaVal;
    public int RegenHpPctDeltaVal;
    public int RegenMpPctDeltaVal;
    public int RegenFpPctDeltaVal;
    public int RegenHpAddDeltaVal;
    public int RegenMpAddDeltaVal;
    public int DRBoostDeltaVal;
    public int APBoostDeltaVal;
    public int BoostDropRateDeltaVal;
    public int CcResistAllDeltaVal;
    public int BoostHatePctDeltaVal;
    public int FlyTimePctDeltaVal;
    public int FlyTimeAddDeltaVal;
    public int FlySpeedStatUpPct;
    public int HealSkillBoostPct;
    public int BoostSkillCostPct;
    public int HuntingXpBoostPct;
    public int GroupHuntingXpBoostPct;
    public int PvpAtkRatioDelta;
    public int PvpDefRatioDelta;
    public int FireResistDelta;
    public int WaterResistDelta;
    public int WindResistDelta;
    public int EarthResistDelta;
    public int StunResistDelta;
    public int StumbleResistDelta;
    public int StaggerResistDelta;
    public int SpinResistDelta;
    public int SleepResistDelta;
    public int FearResistDelta;
    public int OpenAerialResistDelta;
    public int RootResistDelta;
    public int SnareResistDelta;
}
