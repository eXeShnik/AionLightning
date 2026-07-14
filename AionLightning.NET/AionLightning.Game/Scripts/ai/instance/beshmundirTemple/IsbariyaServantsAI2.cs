// IsbariyaServantsAI2 — Java ai/instance/beshmundirTemple/IsbariyaServantsAI2.java. Temporary add
// that self-despawns after a fixed lifetime (longer for one specific npc id).
using AionLightning.Game.Ai;

namespace Ai;

[AiName("isbariyaServants")]
public sealed class IsbariyaServantsAI2 : AggressiveNpcAI2
{
    private const int LongLifetimeNpcId = 281659;
    private const int LongLifetimeMs = 20000;
    private const int ShortLifetimeMs = 10000;

    public override void OnSpawned()
    {
        base.OnSpawned();
        var lifetimeMs = Owner.Template.NpcId == LongLifetimeNpcId ? LongLifetimeMs : ShortLifetimeMs;
        ScheduleTask(() =>
        {
            // note: Java deleted itself via AI2Actions.deleteOwner; no scripted despawn API exists yet.
        }, lifetimeMs);
    }
}
