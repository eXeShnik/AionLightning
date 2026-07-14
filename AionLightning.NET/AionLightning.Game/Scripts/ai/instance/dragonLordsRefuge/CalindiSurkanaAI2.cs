// CalindiSurkanaAI2 — Java ai/instance/dragonLordsRefuge/CalindiSurkanaAI2.java. Calindi's reflector
// add: periodically shouts and applies a reflect effect back onto Calindi.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("calindisurkana")]
// 730695, 730696
public sealed class CalindiSurkanaAI2 : NpcAi2
{
    private Npc? _calindi;

    public override void OnSpawned()
    {
        base.OnSpawned();
        _calindi = GetNpc(219359);
        Reflect();
    }

    private void Reflect()
    {
        ScheduleTask(() =>
        {
            // note: Java shouted (1401543) via NpcShoutsService and applied a reflect effect directly onto
            // _calindi via SkillEngine.applyEffectDirectly; neither NPC shouts nor direct-effect application
            // are exposed to scripts yet. Java's pollInstance also refused decay/respawn/reward for this
            // add — no C# poll equivalent exists.
        }, 3000, 10000);
    }

    public override void OnDied()
    {
        base.OnDied();
    }
}
