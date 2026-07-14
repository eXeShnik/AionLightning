// AggressiveNpcAI2 — Java ai/AggressiveNpcAI2.java. Root ai2 delegator for aggressive NPCs
// (aggro/creature-see/creature-moved forwarded to framework handlers, same as GeneralNpcAI2).
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("aggressive")]
public class AggressiveNpcAI2 : GeneralNpcAI2
{
    public override void OnCreatureSee(Creature creature)
    {
        // note: Java delegated to CreatureEventHandler.onCreatureSee.
    }

    public override void OnCreatureMoved(Creature creature)
    {
        // note: Java delegated to CreatureEventHandler.onCreatureMoved.
    }

    public override void OnCreatureAggro(Creature creature)
    {
        // note: Java delegated to AggroEventHandler.onAggro (guarded by canThink(), which has no C#
        // equivalent); aggro tracking is owned by NpcAiService.
    }

    /// <summary>Java <c>callForHelp</c>: broadcasts a CREATURE_AGGRO event to same-scope NPCs within
    /// <paramref name="distance"/> meters so they aggro the caller's most-hated target.</summary>
    protected void CallForHelp(int distance)
    {
        // note: aggro-list/known-list broadcast isn't tracked by NpcAi2; NPC aggro spreading stays owned
        // by NpcAiService.
    }

    /// <summary>Java <c>getRandomTarget</c>: a random living player from the known-list within 50m.</summary>
    protected Player? GetRandomTarget() => null; // note: known-list/aggro-list membership isn't exposed to scripts yet.
}
