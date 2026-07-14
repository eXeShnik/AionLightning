// DistortedSpaceAI2 — Java ai/instance/tiamatStrongHold/DistortedSpaceAI2.java. Zone hazard: spams
// a skill every 2s for 8s, then self-kills.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("distortedspace")]
public sealed class DistortedSpaceAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        UseSkillLoop();
    }

    private void UseSkillLoop()
    {
        ScheduleTask(() =>
        {
            if (Owner.Template.NpcId == 283232) UseSkill(20740);
        }, 500, 2000);

        ScheduleTask(() =>
        {
            CancelTasks();
            if (Owner.Template.NpcId == 283232) UseSkill(20742);
            // note: Java killed the owner (getController().die()) here — no scripted self-kill API
            // exists yet.
        }, 8000);
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java called AI2Actions.deleteOwner here — no scripted NPC-removal API exists yet.
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
        CancelTasks();
    }

    // note: Java overrode pollInstance to refuse SHOULD_DECAY/SHOULD_RESPAWN/SHOULD_REWARD — no C#
    // equivalent poll exists.
}
