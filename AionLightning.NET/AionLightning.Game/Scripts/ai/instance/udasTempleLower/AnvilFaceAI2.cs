// AnvilFaceAI2 — Java ai/instance/udasTempleLower/AnvilFaceAI2.java. Anvilface boss: shouts on first
// attack and on death (its 50%/25% HP-threshold hooks were empty bodies in Java).
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("anvilface")]
public sealed class AnvilFaceAI2 : AggressiveNpcAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: Java shouted message 1500036 via NpcShoutsService on the first attack after spawn/back-home
        // (its 50%/25% HP threshold hooks were empty bodies); NPC shouts aren't wired at the script layer yet.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java shouted message 1500037 via NpcShoutsService on death.
    }
}
