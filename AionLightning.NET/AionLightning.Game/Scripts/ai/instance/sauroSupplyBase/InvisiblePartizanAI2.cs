// InvisiblePartizanAI2 — Java ai/instance/sauroSupplyBase/InvisiblePartizanAI2.java. Sauro Supply
// Base invisible partizan trap: self-buffs shortly after spawning.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("partizan_invisible")]
public sealed class InvisiblePartizanAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        DaggerBless(3000);
        DaggerBless(4000);
        base.OnSpawned();
    }

    private void Bless(int skillId) => UseSkill(skillId, 60);

    private void DaggerBless(int time) => ScheduleTask(() =>
    {
        if (time == 3000) Bless(21135);
        if (time == 4000) Bless(20251);
    }, time);
}
