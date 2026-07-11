namespace AionLightning.Game.Model.Summons;

/// <summary>Spiritmaster summon behavior mode. Values mirror the Java client wire ids exactly
/// (com.aionemu.gameserver.model.summons.SummonMode) so CM_SUMMON_COMMAND's raw byte can be cast directly.</summary>
public enum SummonMode
{
    Attack  = 0,
    Guard   = 1,
    Rest    = 2,
    Release = 3,
}
