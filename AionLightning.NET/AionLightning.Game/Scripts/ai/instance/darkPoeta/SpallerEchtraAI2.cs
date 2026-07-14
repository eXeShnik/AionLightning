// SpallerEchtraAI2 — Java ai/instance/darkPoeta/SpallerEchtraAI2.java. Boss that paralyzes itself
// and chains a two-skill sequence when a nearby "Drana Lump" npc is within range.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("spaller_echtra")]
public sealed class SpallerEchtraAI2 : AggressiveNpcAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckDirection();
    }

    private void CheckDirection()
    {
        // note: Java enumerated every instance npc 281178 within 2m (WorldMapInstance.getNpcs +
        // MathUtil.getDistance), then triggered TalkEventHandler.onTalk, applied a paralyze effect
        // directly (AI2Actions.applyEffect/SkillData), aborted its current skill/move, and set the
        // PARALYZE abnormal state before chaining two delayed skill casts (18534, then 18574).
        // Multi-npc-by-id enumeration and direct effect/abnormal-state application aren't wired at
        // the script layer yet.
    }

    public override void OnBackHome()
    {
        CancelTasks();
        base.OnBackHome();
    }

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }

    public override void OnDied()
    {
        base.OnDied(); // cancels any scheduled tasks
    }
}
