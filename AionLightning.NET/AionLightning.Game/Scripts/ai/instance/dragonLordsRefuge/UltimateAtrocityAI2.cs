// UltimateAtrocityAI2 — Java ai/instance/dragonLordsRefuge/UltimateAtrocityAI2.java. Tiamat's
// atrocity-event markers: repeatedly cast their zone skill for 11s then self-delete.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("ultimateatrocity")]
// 283237
public sealed class UltimateAtrocityAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        int npcId = getOwner().Template.NpcId;
        int skill = npcId switch
        {
            283237 => 20598,
            283244 => 21160,
            283241 => 21156,
            _ => 0,
        };
        if (skill == 0) return;

        ScheduleTask(() => UseSkill(skill), 0, 2000);
        // note: Java self-deleted via AI2Actions.deleteOwner 11s after spawn; no scripted despawn API
        // exists yet.
        ScheduleTask(() => { }, 11000);
    }

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }
}
