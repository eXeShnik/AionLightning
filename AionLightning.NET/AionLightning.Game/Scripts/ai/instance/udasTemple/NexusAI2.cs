// NexusAI2 — Java ai/instance/udasTemple/NexusAI2.java. Nexus boss: shouts on first attack, then
// shouts and casts a self damage-buff at 50% HP; shouts on death.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("nexus")]
public sealed class NexusAI2 : AggressiveNpcAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: Java shouted message 1500025 on the first attack after spawn/back-home, then at 50% HP
        // shouted 1500026 and cast a self damage-buff skill (18605) via AI2Actions; NPC shouts and
        // AI2Actions skill-casts aren't wired at the script layer yet.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java shouted message 1500027 on death.
    }
}
