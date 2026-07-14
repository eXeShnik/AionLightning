// SteelRakeKeyBoxAI2 — Java ai/instance/steelRake/SteelRakeKeyBoxAI2.java. Chest that self-despawns
// three minutes after spawning if left unopened.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("steel_rake_key_box")]
public sealed class SteelRakeKeyBoxAI2 : ChestAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(() =>
        {
            // note: Java self-despawned this chest via AI2Actions.deleteOwner (guarded by
            // isAlreadyDead()/isSpawned()); self-delete isn't wired at the script layer yet.
        }, 180000);
    }
}
