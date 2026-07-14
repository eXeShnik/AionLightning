// BigBadaboomAI2 — Java ai/instance/steelRose/BigBadaboomAI2.java. Big/Bigger Badaboom use-item:
// morphs the player and self-deletes.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("big_badaboom")]
public sealed class BigBadaboomAI2 : ActionItemNpcAI2
{
    protected override void HandleUseItemFinish(Player player)
    {
        // note: Java stopped the player's protection-active task, then cast a morph skill
        // (0x4E502E = skill 20176 level 46) on the player via SkillEngine.useNoAnimationSkill and
        // deleted the owner (AI2Actions.deleteOwner). Player protection tasks, external-target skill
        // casts, and scripted despawn aren't exposed to the script layer yet.
    }
}
