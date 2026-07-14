// CollapsedReianBuildingAI2 — Java ai/instance/rentusBase/CollapsedReianBuildingAI2.java. Static prop:
// casts a one-shot skill on spawn.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("collapsed_reian_building")]
public sealed class CollapsedReianBuildingAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        UseSkill(20088);
    }
}
