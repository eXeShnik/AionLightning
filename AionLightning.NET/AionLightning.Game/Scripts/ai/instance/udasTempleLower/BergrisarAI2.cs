// BergrisarAI2 — Java ai/instance/udasTempleLower/BergrisarAI2.java. Bergrisar boss: shouts on first
// attack and on death (its 50% HP-threshold hook was an empty body in Java).
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("bergrisar")]
public sealed class BergrisarAI2 : AggressiveNpcAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: Java shouted message 1500040 via NpcShoutsService on the first attack after spawn/back-home
        // (its 50% HP threshold hook was an empty body); NPC shouts aren't wired at the script layer yet.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java shouted message 1500041 via NpcShoutsService on death.
    }
}
