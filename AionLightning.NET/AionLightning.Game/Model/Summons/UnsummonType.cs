namespace AionLightning.Game.Model.Summons;

/// <summary>Reason a summon is being dismissed — controls which system message (if any) the
/// master sees during the release sequence (mirrors Java com.aionemu.gameserver.model.summons.UnsummonType).</summary>
public enum UnsummonType
{
    /// <summary>Master explicitly issued a RELEASE command (CM_SUMMON_COMMAND).</summary>
    Command,

    /// <summary>Reserved for Phase 2 — summon left the master's notify range.</summary>
    Distance,

    /// <summary>Master disconnected — summon is deleted silently, no packets to the (already gone) owner.</summary>
    Logout,

    /// <summary>Timed summon expired (skill's &lt;summon time="N"/&gt; elapsed) or it died.</summary>
    Unspecified,
}
