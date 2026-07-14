// SealGeneratorAI2 — Java ai/instance/raksang/SealGeneratorAI2.java. Static seal prop: shouts once
// when a player wanders close.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("seal_generator")]
public sealed class SealGeneratorAI2 : AggressiveNpcAI2
{
    private bool _startedEvent;

    // note: Java also overrode canThink to always return false — no C# equivalent hook exists.

    public override void OnCreatureMoved(Creature creature)
    {
        if (creature is not Player player) return;
        if (getOwner().Position.DistanceTo(player.Position) > 30) return;
        if (_startedEvent) return;
        _startedEvent = true;
        // note: Java shouted 1401156 via NpcShoutsService here; NPC shouts aren't exposed to scripts yet.
    }

    // note: Java also overrode ask (CAN_RESIST_ABNORMAL = POSITIVE) and modifyDamage (clamped to 1) —
    // neither hook exists on NpcAi2.
}
