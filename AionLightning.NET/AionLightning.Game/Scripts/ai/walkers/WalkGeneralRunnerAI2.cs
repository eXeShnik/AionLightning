// WalkGeneralRunnerAI2 — Java ai/walkers/WalkGeneralRunnerAI2.java. Root base for scripted walker
// NPCs: re-equips the visible weapon once a waypoint move finishes.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("generalrunner")]
public class WalkGeneralRunnerAI2 : GeneralNpcAI2
{
    public override void OnMoveArrived()
    {
        base.OnMoveArrived();
        Owner.State |= CreatureState.WeaponEquipped;
    }
}
