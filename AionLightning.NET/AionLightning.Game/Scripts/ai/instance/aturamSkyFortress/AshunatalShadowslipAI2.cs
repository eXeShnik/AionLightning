// AshunatalShadowslipAI2 — Java ai/instance/aturamSkyFortress/AshunatalShadowslipAI2.java. Aturam
// Sky Fortress boss: opens doors on first attack, then at 50% HP casts a summon skill and walks a
// scripted despawn sequence.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("ashunatal_shadowslip")]
public sealed class AshunatalShadowslipAI2 : AggressiveNpcAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: Java opened instance doors 17/2 on first attack. Once HP dropped to <=50% (first time
        // only) it cast skill 19428, then after 2s cast skill 19417, then after another 3s spawned a
        // helper npc (219186), disabled its own think loop (canThink, no C# equivalent), switched to a
        // scripted walk route (walker id 3002400001), broadcast an emote, and deleted itself via
        // AI2Actions.deleteOwner after 4s. Door control, LifeStats percentage, SkillEngine casting and
        // WalkManager aren't exposed to scripts yet.
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        // note: Java reset its "already triggered"/"summoned" flags, re-opened door 17, closed door 2,
        // and deleted its spawned helper (219186) if present.
    }
}
