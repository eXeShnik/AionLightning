// AcidicSecretionAI2 — Java ai/instance/rentusBase/AcidicSecretionAI2.java. Rentus Base add: casts a
// self-targeted acid skill on a repeating timer until it dies.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("acidic_secretion")]
public sealed class AcidicSecretionAI2 : GeneralNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(() =>
        {
            if (!Owner.IsAlreadyDead) UseSkill(19651);
        }, 1000, 3000);
    }

    public override void OnDied()
    {
        base.OnDied();
    }

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }
}
