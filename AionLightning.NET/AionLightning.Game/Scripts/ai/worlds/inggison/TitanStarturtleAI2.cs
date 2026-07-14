// TitanStarturtleAI2 — Java ai/worlds/inggison/TitanStarturtleAI2.java. Spawns a wind-stream npc on
// death after removing a stray one on spawn.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("titanstarturtle")]
public sealed class TitanStarturtleAI2 : AggressiveNpcAI2
{
    public override void OnDied()
    {
        base.OnDied();
        Spawn(700545, 338.149f, 573.55f, 460f);
        // note: Java also broadcast SM_SYSTEM_MESSAGE(1400486) here; PacketSendUtility isn't wired at the
        // script layer yet.
    }

    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java deleted an existing wind-stream npc (700545) in scope via NpcController.delete(); no
        // npc-deletion hook is exposed to scripts yet (GetNpc only looks it up, it can't remove it).
    }
}
