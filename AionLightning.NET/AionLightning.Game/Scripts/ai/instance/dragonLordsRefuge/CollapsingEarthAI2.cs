// CollapsingEarthAI2 — Java ai/instance/dragonLordsRefuge/CollapsingEarthAI2.java. Ground hazard:
// casts its skill 3s after spawning then force-kills itself.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("collapsingearth")]
// 282737
public sealed class CollapsingEarthAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(() =>
        {
            UseSkill(20173);
            // note: Java also force-killed itself via getController().die(); no scripted self-kill API
            // exists yet.
        }, 3000);
    }
}
