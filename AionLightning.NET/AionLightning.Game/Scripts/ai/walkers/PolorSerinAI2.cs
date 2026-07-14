// PolorSerinAI2 — Java ai/walkers/PolorSerinAI2.java. Walker NPC that pauses its route (unequips its
// weapon) while either of two adult NPCs is nearby, resuming once they leave aggro range.
using System.Linq;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("polorserin")]
public sealed class PolorSerinAI2 : WalkGeneralRunnerAI2
{
    private static readonly int[] StopAdults = { 203129, 203132 };

    public override void OnMoveArrived()
    {
        bool adultsNear = StopAdults
            .Select(GetNpc)
            .Any(npc => npc is not null && Owner.Position.DistanceTo(npc.Position) <= Owner.Template.AggroRange);

        if (adultsNear)
        {
            // note: Java called MoveEventHandler.onMoveArrived directly here (framework plumbing, no C#
            // equivalent) instead of the base WalkGeneralRunnerAI2 hook.
            Owner.State &= ~CreatureState.WeaponEquipped;
        }
        else
        {
            base.OnMoveArrived();
        }
    }
}
