// UnfaithfulNtuamuAI2 — Java ai/worlds/brusthonin/UnfaithfulNtuamuAI2.java. Spawns a replacement npc
// and self-destructs past 50% HP.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("unfaithfulntuamu")]
public sealed class UnfaithfulNtuamuAI2 : AggressiveNpcAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckPercentage(Owner.HpPercentage);
    }

    private void CheckPercentage(int hpPercentage)
    {
        if (hpPercentage > 50) return;
        Spawn(214583, Owner.Position.X, Owner.Position.Y, Owner.Position.Z, (byte)Owner.Position.Heading);
        // note: Java then called AI2Actions.deleteOwner(this) to remove itself; no owner-delete hook is
        // exposed to scripts yet.
    }
}
