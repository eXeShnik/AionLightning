// DrakanHealingServantAI2 — Java ai/classNpc/DrakanHealingServantAI2.java. Targets its creator on
// spawn then loops a heal skill.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("drakanhealingservant")]
public sealed class DrakanHealingServantAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java targeted its creator after 2s then looped skill 20520 (heal) every 6s via
        // ThreadPoolManager (also overrode pollInstance to refuse decay/respawn/reward — no C# equivalent
        // poll exists). Creator-target resolution and repeating skill casts aren't wired at the script
        // layer yet (see NpcAi2.UseSkill).
    }
}
