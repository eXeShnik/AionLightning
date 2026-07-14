// PashidLabUnitSentinelAI2 — Java ai/instance/danuarMysticarium/PashidLabUnitSentinelAI2.java.
// Timed sentinel helper: self-deletes after a per-npcId lifetime.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("pashid_lab_unit_sentinel")]
public sealed class PashidLabUnitSentinelAI2 : AggressiveNpcAI2
{
    private const int ShortLifetimeNpcId = 230077;

    public override void OnSpawned()
    {
        base.OnSpawned();
        int lifetimeMs = Owner.Template.NpcId == ShortLifetimeNpcId ? 20000 : 600000;
        ScheduleTask(() =>
        {
            // note: Java despawned the owner via AI2Actions.deleteOwner here; no scripted despawn API
            // exists yet.
        }, lifetimeMs);
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java also called AI2Actions.deleteOwner(this) here; no scripted despawn API exists
        // yet.
    }
}
