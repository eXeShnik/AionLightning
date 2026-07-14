// AncientWindStreamActivatorAI2 — Java ai/worlds/sarpan/AncientWindStreamActivatorAI2.java. Opens a
// wind-stream event chain: spawns/despawns escort npcs, gated by a door and announce state Java tracked
// via WindstreamTemplate/SM_WINDSTREAM_ANNOUNCE.
using AionLightning.Game.Ai;

namespace Ai;

// note: Java also overrode modifyDamage to clamp incoming damage to 1; no damage-modification hook
// exists on NpcAi2 yet.
[AiName("ancient_windstream_activator")]
public sealed class AncientWindStreamActivatorAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java closed map-region door 146, broadcast a wind-stream-state announce + system message
        // (1401332), and despawned an existing npc 207089 in scope; doors, wind-stream template state,
        // and known-list-wide npc deletion aren't exposed to scripts yet.
        Spawn(207081, 162.31667f, 2210.9192f, 555.0005f);
    }

    private void StartTask()
    {
        ScheduleTask(() =>
        {
            Spawn(207088, 158.58449f, 2204.0615f, 556.51917f);
            Spawn(207089, 158.58449f, 2204.0615f, 556.51917f);
            // note: Java also broadcast a wind-stream-state announce, SM_SYSTEM_MESSAGE(1401331), and an
            // SM_WINDSTREAM_ANNOUNCE(163) here, then deleted both the temporary escort npc and the
            // caller's own spawned npc; packet broadcast and npc deletion aren't exposed to scripts yet.
        }, 15000);
    }

    public override void OnDied()
    {
        Spawn(207087, 158.58449f, 2204.0615f, 556.51917f);
        // note: Java also broadcast SM_SYSTEM_MESSAGE(1401330), reopened door 146, and despawned npc
        // 207081; doors, packet broadcast, and known-list npc deletion aren't exposed to scripts yet.
        base.OnDied();
        // note: Java then called AI2Actions.deleteOwner(this) before scheduling StartTask; no owner-delete
        // hook is exposed to scripts yet.
        StartTask();
    }
}
