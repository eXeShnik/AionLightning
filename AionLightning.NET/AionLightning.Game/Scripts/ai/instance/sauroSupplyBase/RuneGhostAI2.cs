// RuneGhostAI2 — Java ai/instance/sauroSupplyBase/RuneGhostAI2.java. Sauro Supply Base rune ghost:
// casts a skill shortly after spawning, then self-deletes.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("rune_ghost")]
public sealed class RuneGhostAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(() =>
        {
            if (!Owner.IsAlreadyDead) UseSkill(21185, 25);
        }, 2000);
        // note: Java also deleted itself (AI2Actions.deleteOwner) 4.5s after spawning when still alive;
        // deleting an NPC from the world isn't exposed to scripts yet.
    }
}
