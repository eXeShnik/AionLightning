// TakunGojiraAI2 — Java ai/instance/empyreanCrucible/TakunGojiraAI2.java. One of a paired duel-boss
// set: hates its counterpart on spawn so the two fight each other.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("takun_gojira")]
public sealed class TakunGojiraAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(() =>
        {
            var counterpart = GetNpc(getOwner().Template.NpcId == 217596 ? 217597 : 217596);
            if (counterpart is not null)
                getOwner().AddHate(counterpart.ObjectId, 1000000);
        }, 500);
    }
}
