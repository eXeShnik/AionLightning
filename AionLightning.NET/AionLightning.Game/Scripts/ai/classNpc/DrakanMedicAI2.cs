// DrakanMedicAI2 — Java ai/classNpc/DrakanMedicAI2.java. Drakan medic: has a chance per attack to
// summon a rating-dependent healing servant, and despawns it on back-home/death.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("drakanmedic")]
public sealed class DrakanMedicAI2 : AggressiveFirstSkillAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: Java had a 3% chance per attack to spawn a rating-dependent holy-servant helper
        // (281621 normal / 281839 other) near itself via VisibleObjectSpawner/NpcShoutsService; helper
        // spawning isn't wired at the script layer yet.
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        // note: Java despawned its spawned holy-servant helper here; helper-spawn tracking isn't ported yet.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: same helper-despawn as OnBackHome.
    }
}
