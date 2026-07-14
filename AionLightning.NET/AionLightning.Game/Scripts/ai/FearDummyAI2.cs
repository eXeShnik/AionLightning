// FearDummyAI2 — Java ai/FearDummyAI2.java. Fleeing training dummy: always takes 1 damage and
// moves away from its attacker along a geo-collision-checked vector.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("fear_dummy")]
public sealed class FearDummyAI2 : GeneralNpcAI2
{
    public override void OnAttack(Creature attacker)
    {
        // note: Java computed a flee vector away from the attacker (heading + move speed), resolved the
        // nearest walkable point via GeoService.getClosestCollision, and moved the owner there; it also
        // overrode modifyDamage to always return 1. Geo/collision queries and damage-modification hooks
        // aren't exposed to scripts yet.
    }
}
