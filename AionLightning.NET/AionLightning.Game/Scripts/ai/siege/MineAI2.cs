// MineAI2 — Java ai/siege/MineAI2.java. Siege mine trap: detonates its skill against whatever
// aggroed it, then self-destructs 1.5s later.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("siege_mine")]
public sealed class MineAI2 : SiegeNpcAI2
{
    public override void OnCreatureAggro(Creature creature)
    {
        UseSkill(18407);
        ScheduleTask(DetonateAndRemove, 1500);
    }

    private void DetonateAndRemove()
    {
        // note: Java deleted its own owner NPC (AI2Actions.deleteOwner) 1.5s after using the mine skill;
        // NPC self-removal isn't exposed to the script layer yet.
    }
}
