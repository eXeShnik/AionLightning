// FireStormAI2 — Java ai/instance/tiamatStrongHold/FireStormAI2.java. Zone hazard: spams a skill
// every second; non-primary spawns (npcId != 283102) self-remove after 20s.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("tahabatafirestorm")]
public sealed class FireStormAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        int skill = Owner.Template.NpcId == 283102 ? 20753 : 20759;
        ScheduleTask(() => UseSkill(skill), 0, 1000);

        if (Owner.Template.NpcId != 283102)
        {
            // note: Java self-removed via getController().onDelete() 20s after spawn — no scripted
            // NPC-removal API exists yet.
        }
    }

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }

    // note: Java overrode pollInstance to refuse SHOULD_DECAY/SHOULD_RESPAWN/SHOULD_REWARD — no C#
    // equivalent poll exists.
}
