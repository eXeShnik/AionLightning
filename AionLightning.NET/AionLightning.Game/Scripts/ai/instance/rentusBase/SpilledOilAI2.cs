// SpilledOilAI2 — Java ai/instance/rentusBase/SpilledOilAI2.java. Ground hazard: casts its skill every
// 4s for 7 rounds, then self-despawns.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("spilled_oil")]
public sealed class SpilledOilAI2 : GeneralNpcAI2
{
    private int _count;

    public override void OnSpawned()
    {
        base.OnSpawned();
        StartEventTask();
    }

    private void StartEventTask()
    {
        ScheduleTask(() =>
        {
            if (Owner.IsAlreadyDead) return;
            _count++;
            if (_count < 7)
            {
                UseSkill(19658);
                StartEventTask();
            }
            else
            {
                // note: Java called AI2Actions.deleteOwner(this) here; scripted NPC delete isn't exposed
                // to scripts yet.
            }
        }, 4000);
    }
}
