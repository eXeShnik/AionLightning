// HaramelChestAI2 — Java ai/instance/haramel/HaramelChestAI2.java. Haramel chest: on despawn, spawns
// a replacement decoration npc at the chest's fixed position.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("haramelchest")]
public sealed class HaramelChestAI2 : ChestAI2
{
    public override void OnDespawned()
    {
        // note: Java only spawned this replacement when getPosition().getWorldMapInstance() != null; that
        // instance-scope guard isn't exposed on NpcAi2, so this always spawns on despawn.
        Spawn(700852, 224.598f, 331.143f, 141.892f, 90);
    }
}
