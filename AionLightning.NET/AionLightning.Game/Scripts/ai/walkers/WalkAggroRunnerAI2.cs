// WalkAggroRunnerAI2 — Java ai/walkers/WalkAggroRunnerAI2.java. Aggressive walker NPC: re-equips
// the visible weapon once a waypoint move finishes.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("aggrorunner")]
public sealed class WalkAggroRunnerAI2 : AggressiveNpcAI2
{
    public override void OnMoveArrived()
    {
        base.OnMoveArrived();
        Owner.State |= CreatureState.WeaponEquipped;
    }
}
