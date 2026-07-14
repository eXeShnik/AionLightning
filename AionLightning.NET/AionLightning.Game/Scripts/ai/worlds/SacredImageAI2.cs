// SacredImageAI2 — Java ai/worlds/SacredImageAI2.java. Casts a race-gated skill on any opposite-race
// player within 25m.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("sacred_image")]
public sealed class SacredImageAI2 : NoActionAI2
{
    public override void OnCreatureSee(Creature creature) => CheckDistance(creature);

    public override void OnCreatureMoved(Creature creature) => CheckDistance(creature);

    private void CheckDistance(Creature creature)
    {
        int spellId = Owner.Template.NpcId == 258281 ? 20373 : 20374;
        if (creature is not Player player) return;
        if ((player.Race == Race.ASMODIANS && Owner.Template.NpcId == 258281)
            || (player.Race == Race.ELYOS && Owner.Template.NpcId == 258280))
            return;
        if (Owner.Position.DistanceTo(creature.Position) <= 25)
            UseSkill(spellId, 65);
        // note: Java also overrode modifyDamage to clamp incoming damage to 1; no damage-modification
        // hook exists on NpcAi2 yet.
    }
}
