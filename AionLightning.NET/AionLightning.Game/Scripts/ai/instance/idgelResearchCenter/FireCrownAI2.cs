// FireCrownAI2 — Java ai/instance/idgelResearchCenter/FireCrownAI2.java. Fire Crown prop: casts a
// repeating aura skill (staggered by variant) starting right after spawn.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("fire_crown")]
public sealed class FireCrownAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        int npcId = getOwner().Template.NpcId;
        int skill = npcId == 284642 ? 21127 : 21128;
        int delay = npcId == 284642 ? 500 : 2000;
        ScheduleTask(() => UseSkill(skill), delay, delay);
        // note: Java also overrode pollInstance to refuse decay/respawn/reward — no C# equivalent poll
        // exists.
    }
}
