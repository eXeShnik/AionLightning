// PletusOrbAI2 — Java ai/instance/argentManor/PletusOrbAI2.java. Wandering orb prop that plays a
// random emote on each waypoint arrival and occasionally spawns a "magical sap" prop nearby.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("pletus_orb")]
public sealed class PletusOrbAI2 : GeneralNpcAI2
{
    public override void OnMoveArrived()
    {
        base.OnMoveArrived();
        // note: Java randomly played one of two emote states via SM_EMOTION, then — if no magical_sap
        // (282148) was within 1m — had a 30% chance to spawn one at its current position. canThink()
        // always returned false (no C# equivalent); also overrode modifyDamage to clamp to 1 and
        // ask(CAN_RESIST_ABNORMAL=POSITIVE) — neither hook exists on NpcAi2. Known-list scanning and
        // SM_EMOTION broadcast aren't exposed to scripts yet.
    }
}
