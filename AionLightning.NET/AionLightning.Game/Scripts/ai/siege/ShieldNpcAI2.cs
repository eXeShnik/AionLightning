// ShieldNpcAI2 — Java ai/siege/ShieldNpcAI2.java. Fortress shield generator: toggles the
// fortress's under-shield flag and broadcasts the shield-effect packet on spawn/despawn.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("siege_shieldnpc")]
public sealed class ShieldNpcAI2 : SiegeNpcAI2
{
    public override void OnDespawned()
    {
        SendShieldPacket(false);
        base.OnDespawned();
    }

    public override void OnSpawned()
    {
        SendShieldPacket(true);
        base.OnSpawned();
    }

    private void SendShieldPacket(bool shieldStatus)
    {
        // note: Java toggled SiegeService's fortress under-shield flag (from the NPC's SiegeSpawnTemplate
        // siege id) and broadcast SM_SHIELD_EFFECT to every player in the instance. SiegeService and
        // SiegeSpawnTemplate aren't exposed to the script layer yet.
    }
}
