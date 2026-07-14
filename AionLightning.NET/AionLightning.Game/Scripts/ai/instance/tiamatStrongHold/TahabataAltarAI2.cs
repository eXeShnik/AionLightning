// TahabataAltarAI2 — Java ai/instance/tiamatStrongHold/TahabataAltarAI2.java. Lava-floor hazard:
// debuffs players caught in its band.
using System.Linq;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("tahabataaltar")]
public sealed class TahabataAltarAI2 : NpcAi2
{
    public override void OnCreatureSee(Creature creature) => CheckDistance(creature);

    public override void OnCreatureMoved(Creature creature) => CheckDistance(creature);

    private void CheckDistance(Creature creature)
    {
        int npcId = Owner.Template.NpcId;
        int debuff = npcId switch
        {
            283118 => 20970,
            283120 => 20971,
            _ => 0,
        };
        if (creature is not Player) return;

        float dist = Owner.Position.DistanceTo(creature.Position);
        bool inBand = (npcId == 283118 && dist is >= 25 and <= 37)
            || (npcId == 283120 && dist is >= 20 and <= 25);
        if (inBand && !HasAbnormalEffect(creature, debuff))
        {
            UseSkill(debuff);
        }
    }

    private static bool HasAbnormalEffect(Creature creature, int skillId) =>
        creature.GetActiveEffects().Any(e => e.SkillId == skillId);

    // note: Java overrode pollInstance to refuse SHOULD_DECAY/SHOULD_RESPAWN/SHOULD_REWARD — no C#
    // equivalent poll exists.
}
