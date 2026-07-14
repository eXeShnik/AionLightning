// PazuzuAI2 — Java ai/instance/abyssal_splinter/PazuzuAI2.java. Abyssal Splinter boss: on first
// attack shouts and starts a repeating skill/worm-add spawn task.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("pazuzu")]
public sealed class PazuzuAI2 : AggressiveNpcAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: Java shouted (342219) and started a 70s-repeating task casting skill 19145 on itself and
        // spawning 5 worm adds (281909, each casting skill 19291) if none were present. NpcShoutsService
        // and instance npc lookup aren't exposed to scripts yet.
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        // note: Java cancelled the repeating task and reset its "already triggered" flag.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java cancelled the repeating task and shouted (1500003).
    }
}
