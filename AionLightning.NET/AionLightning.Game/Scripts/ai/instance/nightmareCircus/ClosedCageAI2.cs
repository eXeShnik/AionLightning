// ClosedCageAI2 — Java ai/instance/nightmareCircus/ClosedCageAI2.java. Cage that forwards a
// player's skill use to a nearby caged npc's own AI, translating the skill id.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("closed_cage")]
public sealed class ClosedCageAI2 : NoActionAI2
{
    public void PlayerSkillUse(Player player, int skillId)
    {
        int skill = skillId switch
        {
            21327 => 21365,
            21328 => 21364,
            _ => 0
        };
        if (skill == 0) return;

        var npc = GetNpc(831573);
        if (npc is null) return;

        if (Owner.Position.DistanceTo(player.Position) <= 10)
        {
            // note: Java targeted+cast a skill through the OTHER npc's own AI2 instance
            // (npc.getAi2().targetCreature/useSkill); commanding another NPC's AI isn't wired at the
            // script layer yet.
        }
    }
}
