// TopazKomadAI2 — Java ai/worlds/tiamaranta/ativasCristalline/TopazKomadAI2.java. Temporary add:
// self-despawns after a fixed lifetime.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("topazKomad")]
public sealed class TopazKomadAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        int lifetime = Owner.Template.NpcId == 282709 ? 20000 : 10000;
        ScheduleTask(() =>
        {
            // note: Java called AI2Actions.deleteOwner(this) here; no owner-delete hook is exposed to
            // scripts yet.
        }, lifetime);
    }
}
