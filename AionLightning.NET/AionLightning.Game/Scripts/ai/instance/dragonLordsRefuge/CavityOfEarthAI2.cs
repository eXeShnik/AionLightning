// CavityOfEarthAI2 — Java ai/instance/dragonLordsRefuge/CavityOfEarthAI2.java. Ground hazard: hits
// nearby players with a debuff, then self-deletes after 10s.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("cavityofearth")]
// 282735
public sealed class CavityOfEarthAI2 : NpcAi2
{
    public override void OnCreatureSee(Creature creature)
    {
        // note: Java checked MathUtil.isIn3dRange(5m) plus an existing-abnormal-effect(20719) guard on
        // nearby players before casting skill 20719; 3d-range checks, abnormal-effect queries, and skill
        // casting aren't exposed to scripts yet.
    }

    public override void OnCreatureMoved(Creature creature)
    {
        // note: same check as OnCreatureSee (Java's shared checkDistance helper).
    }

    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(() =>
        {
            // note: Java self-deleted via getController().onDelete() after 10s; no scripted despawn API
            // exists yet. Java's pollInstance also refused decay/respawn/reward — no C# poll equivalent.
        }, 10000);
    }
}
