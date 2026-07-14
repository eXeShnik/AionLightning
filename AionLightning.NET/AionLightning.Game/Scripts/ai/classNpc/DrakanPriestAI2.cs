// DrakanPriestAI2 — Java ai/classNpc/DrakanPriestAI2.java. Drakan priest: has a chance per attack
// to summon 1-3 healing servants, and despawns them on back-home/death.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("xdrakanpriest")]
public sealed class DrakanPriestAI2 : AggressiveNpcAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: Java had a 3% chance per attack to spawn 1-3 healing-servant helpers (282988) near itself
        // via VisibleObjectSpawner/NpcShoutsService; helper spawning isn't wired at the script layer yet.
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        // note: Java despawned its spawned healing-servant helpers here; helper-spawn tracking isn't
        // ported yet.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: same helper-despawn as OnBackHome.
    }
}
