// RukrilAI2 — Java ai/instance/abyssal_splinter/RukrilAI2.java. Abyssal Splinter boss: once below
// 95% HP, starts a repeating skill cast that also tops up its shade adds if they're gone.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("rukril")]
public sealed class RukrilAI2 : AggressiveNpcAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: Java checked HP percentage and, once <=95% (first time only), started a 70s-repeating
        // task casting a no-animation skill (19266) and respawning two shade adds (281907) if none were
        // present. LifeStats percentage and WorldMapInstance npc lookup aren't exposed to scripts yet.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java cancelled the repeating skill task.
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        // note: Java cancelled the repeating skill task, reset its threshold flag, and removed a tracked
        // skill effect (19266). EffectController isn't exposed to scripts yet.
    }
}
