// CalindiSummonsAI2 — Java ai/instance/dragonLordsRefuge/CalindiSummonsAI2.java. Short-lived Calindi
// summon: repeatedly casts its trigger skill then self-deletes after 15s.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("calindisummon")]
// 283131, 283132
public sealed class CalindiSummonsAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        int npcId = getOwner().Template.NpcId;
        int skill = npcId == 283132 ? 20914 : 20916;
        int delay = npcId == 283132 ? 500 : 2000;
        ScheduleTask(() => UseSkill(skill), delay, delay);

        // note: Java self-deleted via getController().onDelete() 15s after spawn; no scripted despawn API
        // exists yet. Java's pollInstance also refused decay/respawn/reward for this summon — no C# poll
        // equivalent exists.
        ScheduleTask(() => { }, 15000);
    }

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }
}
