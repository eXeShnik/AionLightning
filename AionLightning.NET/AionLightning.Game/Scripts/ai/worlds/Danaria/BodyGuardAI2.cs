// BodyGuardAI2 — Java ai/worlds/Danaria/BodyGuardAI2.java. Siege bodyguard: watches for enemy-race
// players casting/casted-at and fires a 4-skill chain at them.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("bodyguard")]
public sealed class BodyGuardAI2 : AggressiveNpcAI2
{
    public override void OnCreatureSee(Creature creature)
    {
        // note: Java delegated to CreatureEventHandler.onCreatureSee/AggroEventHandler.onAggro (aggro
        // tracking is owned by NpcAiService), then gated a 4-skill chain (StartChain: 20672/20542/20548/
        // 21263 vs the aggressor) behind creature.getSkillNumber()/getCastingSkillId()/isCasting() and a
        // target-race check, toggling its own canThink() flag. None of those creature-cast-state
        // accessors, canThink() gating, or generic Creature.getRace() exist on Creature/NpcAi2 yet.
    }
}
