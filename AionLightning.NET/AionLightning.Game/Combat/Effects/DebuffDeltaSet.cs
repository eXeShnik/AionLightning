using AionLightning.Game.Model;

namespace AionLightning.Game.Combat.Effects;

/// <summary>
/// S4d: pure debuff-delta output of <see cref="DebuffEffectCalculator.Compute"/> — mirrors the ~40
/// locals that CM_CASTSPELL's enemy-debuff block used to compute inline before folding them into a
/// single <see cref="AbnormalState"/>. Fields are mutable so the calculator can accumulate
/// (flat ADD + percent-of-target-base) exactly like the original locals did.
/// </summary>
public sealed class DebuffDeltaSet
{
    public AbnormalCcFlags CcFlags;
    public int MovSpeedPct;
    public int AttackSpeedPct;
    public int FlySpeedDebuffPct;
    public int BlindDodgePct;
    public int HealReceivedPctDelta;

    public int PdefDelta;
    public int MResistDelta;
    public int PatkDelta;
    public int EvasionDelta;
    public int MagicAtkDelta;
    public int AtkSpeedDelta;
    public int MaxHpDelta;
    public int MaxMpDelta;
    public int MagicBoostDeltaVal;
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
    public int MagicDefDeltaVal;

    public int FireResistDelta;
    public int WaterResistDelta;
    public int WindResistDelta;
    public int EarthResistDelta;

    public int SleepResistDelta;
    public int RootResistDelta;
    public int StunResistDelta;
    public int StumbleResistDelta;
    public int StaggerResistDelta;
    public int SpinResistDelta;
    public int SnareResistDelta;
    public int FearResistDelta;
    public int OpenAerialResistDelta;

    public int ResurrectBaseSkillId;
    public string DispelCategory = "NONE";
    public int ReqDispelLevel;
}
