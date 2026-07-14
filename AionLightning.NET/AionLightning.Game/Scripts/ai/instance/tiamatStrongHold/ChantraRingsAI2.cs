// ChantraRingsAI2 — Java ai/instance/tiamatStrongHold/ChantraRingsAI2.java. Trap ring hazard:
// debuffs players caught in its band, then self-removes after 20s.
using System.Linq;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("chantrarings")]
public sealed class ChantraRingsAI2 : NpcAi2
{
    public override void OnCreatureSee(Creature creature) => CheckDistance(creature);

    public override void OnCreatureMoved(Creature creature) => CheckDistance(creature);

    private void CheckDistance(Creature creature)
    {
        int npcId = Owner.Template.NpcId;
        int debuff = npcId == 283172 ? 20735 : 20734;
        if (creature is not Player) return;

        float dist = Owner.Position.DistanceTo(creature.Position);
        bool inBand = (npcId == 283172 && dist is >= 10 and <= 18)
            || (npcId == 283171 && dist is >= 18 and <= 25)
            || (npcId == 283171 && dist is >= 0 and <= 10);
        if (inBand && !HasAbnormalEffect(creature, debuff))
        {
            UseSkill(debuff);
        }
    }

    private static bool HasAbnormalEffect(Creature creature, int skillId) =>
        creature.GetActiveEffects().Any(e => e.SkillId == skillId);

    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java self-removed via getController().onDelete() 20s after spawn — no scripted
        // NPC-removal API exists yet.
    }

    // note: Java overrode pollInstance to refuse SHOULD_DECAY/SHOULD_RESPAWN/SHOULD_REWARD — no C#
    // equivalent poll exists.
}
