// TiamatEyeAI2 — Java ai/instance/tiamatStrongHold/TiamatEyeAI2.java. Story NPC: shouts a
// variant-specific line on spawn, then self-removes after 5s if still alive.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("tiamateye")]
public sealed class TiamatEyeAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        switch (Owner.Template.NpcId)
        {
            case 283177:
                SendMsg(1500679);
                break;
            case 283178:
                SendMsg(1500680);
                break;
            case 283179:
                SendMsg(1500681);
                break;
            case 283180:
                SendMsg(1500682);
                break;
        }
        Despawn();
    }

    private void Despawn()
    {
        ScheduleTask(() =>
        {
            if (!Owner.IsAlreadyDead)
            {
                // note: Java deleted the owner (AI2Actions.deleteOwner) here — no scripted
                // NPC-removal API exists yet.
            }
        }, 5000);
    }
}
