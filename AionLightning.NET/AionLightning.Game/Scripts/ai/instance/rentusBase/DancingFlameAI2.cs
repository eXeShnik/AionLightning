// DancingFlameAI2 — Java ai/instance/rentusBase/DancingFlameAI2.java. Rentus Base trap chain (npc ids
// 282996-282999): the first two poll for nearby players and spawn a "step" npc; the "step" npcs cast a
// skill then self-despawn after a few seconds.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("dancing_flame")]
public sealed class DancingFlameAI2 : GeneralNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        if (Owner.Template.NpcId is 282996 or 282997)
        {
            // note: Java polled getKnownList().getKnownPlayers() every 3s and spawned a matching "step"
            // npc (282998/282999) when a player stood within 30m — known-list membership isn't exposed to
            // scripts yet.
        }
        else
        {
            ScheduleTask(() => UseSkill(Owner.Template.NpcId == 282998 ? 20536 : 20535), 500);
            ScheduleTask(Despawn, 4000);
        }
    }

    private void Despawn()
    {
        // note: Java called AI2Actions.deleteOwner(this) here; scripted NPC delete isn't exposed to
        // scripts yet.
    }

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }

    public override void OnDied()
    {
        base.OnDied();
    }
}
