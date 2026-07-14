// HomingNpcAI2 — Java ai/HomingNpcAI2.java. Homing projectile NPC that doesn't think to return
// home and reschedules its next attack while its active skill remains set.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("homing")]
public sealed class HomingNpcAI2 : GeneralNpcAI2
{
    public override void OnThink()
    {
        // homings are not thinking to return (matches Java's empty think() override).
    }

    public override AttackIntention ChooseAttackIntention()
        => AttackIntention.SimpleAttack; // note: Java left a TODO for skill-type homings.

    public override void OnAttackComplete()
    {
        base.OnAttackComplete();
        // note: Java rescheduled the next attack via AttackManager when the owning Homing still had an
        // active skill id (also overrode pollInstance to refuse decay/respawn/reward — no C# equivalent
        // poll exists). The Homing model and AttackManager aren't ported to the script layer yet.
    }
}
