// DranaLumpAI2 — Java ai/instance/darkPoeta/DranaLumpAI2.java. Use-item npc that casts a
// paralyze-break skill on the player and self-deletes.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("dranalump")]
public sealed class DranaLumpAI2 : ActionItemNpcAI2
{
    protected override void HandleUseItemFinish(Player player)
    {
        int skillId = Owner.Template.NpcId == 281178 ? 18536 : 0; // Drana Break
        // note: Java cast skillId(level 46) directly on the player via SkillEngine.getSkill(...).useSkill();
        // skill-casting on a player target isn't wired at the script layer yet.
        _ = skillId;
        // note: Java also self-deleted via getController().onDelete(); self-delete isn't wired at the
        // script layer yet.
    }
}
