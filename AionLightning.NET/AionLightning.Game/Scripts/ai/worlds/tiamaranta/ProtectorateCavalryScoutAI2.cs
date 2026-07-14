// ProtectorateCavalryScoutAI2 — Java ai/worlds/tiamaranta/ProtectorateCavalryScoutAI2.java. Spawns a
// walking event npc on spawn and re-chains another when it reaches a walker checkpoint.
using System;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("protectorate_cavalry_scout")]
public sealed class ProtectorateCavalryScoutAI2 : NpcAi2
{
    private int _size;

    public override void OnSpawned()
    {
        base.OnSpawned();
        SpawnEventNpc();
    }

    private void SpawnEventNpc()
    {
        _size++;
        int npcId = Random.Shared.Next(1, 4) switch
        {
            1 => 799991,
            2 => 799992,
            _ => 799993,
        };
        Spawn(npcId, 131.34761f, 2770.1194f, 293.92636f, 100);
        // note: Java also assigned a walker route (6000300001), started WalkManager walking, set the
        // owner's move state, broadcast an SM_EMOTION(START_EMOTE2), and randomly shouted one of two
        // messages via NpcShoutsService; walker routes, WalkManager, and packet broadcast aren't exposed
        // to scripts yet.
    }

    public override void OnCreatureMoved(Creature creature)
    {
        // note: Java watched its spawned event npcs (799991-799993) for walker checkpoint 4 (spawn
        // another, up to 2 concurrent) or checkpoint 0 (abort/stop walking and delete itself); walker-
        // checkpoint tracking and npc deletion aren't exposed to scripts yet.
        base.OnCreatureMoved(creature);
    }
}
