// TriroansSummonAI2 — Java ai/instance/theobomosLab/TriroansSummonAI2.java. Triroan's elemental
// summon: walks to a boss-side elemental point, then buffs the Unstable Triroan boss and despawns.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("triroan_summon")]
public sealed class TriroansSummonAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java picked a walk-target point and helper buff skill id from its npcId (280975-280978);
        // it never thinks (canThink() == false, no C# equivalent hook).
    }

    public override void OnMoveArrived()
    {
        // note: Java checked getMoveController().getCurrentPoint() against its walk target, then cast its
        // helper buff skill on the Unstable Triroan boss (214669) via SkillEngine and scheduled its own
        // despawn 3s later; move-controller waypoint tracking and skill casting aren't wired at the script
        // layer yet.
    }
}
