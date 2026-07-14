// GatesSummonedAI2 — Java ai/instance/abyssal_splinter/GatesSummonedAI2.java. Summoned add that
// walks to the boss npc on spawn, then alternates between two skills on it every 30s.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("gatessummoned")]
public sealed class GatesSummonedAI2 : GeneralNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java disabled its own think loop (canThink, no C# equivalent), stopped its attack emote,
        // set state FOLLOWING, targeted the boss npc (216960) via AI2Actions.targetCreature, and moved
        // to it. WorldMapInstance npc lookup, EmoteManager and AI2Actions aren't exposed to scripts yet.
    }

    public override void OnDied()
    {
        // note: Java cancelled the repeating event task before calling base.handleDied().
        base.OnDied();
    }

    public override void OnDespawned()
    {
        // note: same event-task cancel as OnDied.
        base.OnDespawned();
    }

    public override void OnMoveArrived()
    {
        base.OnMoveArrived();
        // note: Java started a 30s-repeating task alternating between two skills (19257/19281) cast on
        // the boss npc, cancelling itself once this npc was already dead and its owner gone. Random
        // selection, SkillEngine casting and instance npc lookup aren't exposed to scripts yet.
    }
}
