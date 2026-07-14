// SiegeTeleporterAI2 — Java ai/siege/SiegeTeleporterAI2.java. Fortress teleporter: toggles the
// fortress's can-teleport flag and broadcasts fortress-info on spawn/despawn.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("siege_teleporter")]
public sealed class SiegeTeleporterAI2 : GeneralNpcAI2
{
    public override void OnDespawned()
    {
        SetCanTeleport(false);
        base.OnDespawned();
    }

    public override void OnSpawned()
    {
        SetCanTeleport(true);
        base.OnSpawned();
    }

    private void SetCanTeleport(bool status)
    {
        // note: Java toggled SiegeService's fortress can-teleport flag (from the owner's SiegeNpc siege id)
        // and broadcast SM_FORTRESS_INFO to every player in the instance. SiegeNpc/SiegeService aren't
        // exposed to the script layer yet.
    }
}
