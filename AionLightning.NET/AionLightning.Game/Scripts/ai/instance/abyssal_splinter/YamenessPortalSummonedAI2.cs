// YamenessPortalSummonedAI2 — Java ai/instance/abyssal_splinter/YamenessPortalSummonedAI2.java.
// Portal prop that spawns a pair of adds near itself 12s after spawn, then repeats every 60s.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("yamenessportal")]
public sealed class YamenessPortalSummonedAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java scheduled a 12s-delayed spawn of two adds (281903/281904) offset from itself, then
        // repeated that spawn every 60s while alive. This is straightforward and could be reimplemented
        // with ScheduleTask + Spawn once the owner-offset spawn positions are confirmed against the
        // ported coordinate system.
    }
}
