// DamageCircleAI2 — Java ai/instance/danuarReliquary/DamageCircleAI2.java. Ring hazard that kills
// any player standing within its damage band.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("damagecircle")]
public sealed class DamageCircleAI2 : NpcAi2
{
    private const int DamageCircleNpcId = 284447;

    public override void OnCreatureSee(Creature creature) => CheckDistance(creature);

    public override void OnCreatureMoved(Creature creature) => CheckDistance(creature);

    private void CheckDistance(Creature creature)
    {
        if (creature is not Player) return;
        if (Owner.Template.NpcId != DamageCircleNpcId) return;
        // note: Java killed the player outright (Creature.getController().die()) once they stood
        // between 16 and 50m of the circle (MathUtil.isIn3dRangeLimited); MathUtil range checks and
        // the death controller aren't exposed to the script layer yet.
    }
}
