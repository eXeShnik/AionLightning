using AionLightning.Game.Model.Summons;
using AionLightning.Game.Model.Templates.Npc;

namespace AionLightning.Game.Model;

/// <summary>
/// A player-controlled summon (Spiritmaster spirit and similar &lt;summon&gt; skill effects).
/// Phase 1 (M381): stats are interim — sourced directly from <see cref="Template"/> (the same
/// NpcTemplate a wild NPC of this npc_id would use) rather than the Java summon_stats templates
/// keyed by (npcId, level). Panel/update numbers will look off until Phase 3 ports summon_stats.
/// </summary>
public sealed class Summon : Creature
{
    public NpcTemplate Template { get; }

    /// <summary>The player who cast the summon skill. Set to null once release completes and the link is torn down.</summary>
    public Player? Master { get; set; }

    public SummonMode Mode { get; set; } = SummonMode.Guard;

    /// <summary>Skill level the summon was cast at (Java Summon.level) — drives the panel's displayed level, distinct
    /// from <see cref="Template"/>'s npc_template level.</summary>
    public byte Level { get; set; }

    /// <summary>Remaining lifetime in seconds from the casting skill's &lt;summon time="N"/&gt; attribute (0 = permanent until released).</summary>
    public int LiveTime { get; set; }

    public Summon(Player master, NpcTemplate template, byte level, int liveTime)
    {
        Master        = master;
        Template      = template;
        Level         = level;
        LiveTime      = liveTime;
        Name          = template.Name;
        MaxHp         = template.MaxHp;
        CurrentHp     = template.MaxHp;
        MovementSpeed = template.Stats?.RunSpeed ?? 6.0f;
    }
}
