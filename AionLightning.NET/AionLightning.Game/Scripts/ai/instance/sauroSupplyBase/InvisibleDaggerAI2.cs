// InvisibleDaggerAI2 — Java ai/instance/sauroSupplyBase/InvisibleDaggerAI2.java. Sauro Supply Base
// invisible dagger trap: self-buffs shortly after spawning.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("dagger_invisible")]
public sealed class InvisibleDaggerAI2 : AggressiveNpcAI2
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
