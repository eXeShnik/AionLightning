// DebilkarimTheMakerAI2 — Java ai/instance/udasTempleLower/DebilkarimTheMakerAI2.java. Debilkarim the
// Maker boss: HP-threshold heal/adds/infernal-rift sequence with tracked helper spawns.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("debilkarimthemaker")]
public sealed class DebilkarimTheMakerAI2 : AggressiveNpcAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: Java shouted message 1500038 on the first attack after spawn/back-home, then at 50/25/10/5%
        // HP ran a heal task (skill 18636 + adds spawn 281420), an infernal-rift teleport sequence (skill
        // 19000 + World.updatePosition to teleport a random nearby player), and repeated adds spawns
        // (215845) tracked in a helper-objectId list; none of SpawnEngine/World.findVisibleObject/
        // AI2Actions.useSkill/PacketSendUtility are wired at the script layer yet.
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        // note: Java cancelled the heal/infernal-rift tasks, despawned tracked helper adds, and cleared all
        // effects on the owner here.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java shouted message 1500039, despawned tracked helper adds, and cancelled the heal/
        // infernal-rift tasks.
    }

    public override void OnDespawned()
    {
        // note: Java despawned tracked helper adds and cancelled the heal/infernal-rift tasks here too.
    }
}
