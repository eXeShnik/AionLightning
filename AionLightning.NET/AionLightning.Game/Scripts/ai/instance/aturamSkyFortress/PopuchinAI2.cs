// PopuchinAI2 — Java ai/instance/aturamSkyFortress/PopuchinAI2.java. Aturam Sky Fortress boss:
// closes a door on first attack and runs a repeating bomb-drop task that shifts pattern once below
// 50% HP.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("popuchin")]
public sealed class PopuchinAI2 : AggressiveNpcAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: Java closed instance door 68 on first attack and started a self-rescheduling bomb task:
        // after 15.5s cast skill 19413 on its current target, after another 3s cast skill 19412 on
        // itself, then after another 1.5s — while still above 50% HP and spawned — spawn 2 bomb props
        // (217374) at its position and reschedule, or below 50% spawn 10 random-scattered bomb props
        // (217375) in a ring and reschedule. LifeStats percentage, door control, target resolution and
        // SkillEngine casting aren't exposed to scripts yet.
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        // note: Java reset its "already engaged" flag, re-opened door 68, and cancelled the bomb task.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java re-opened instance door 68.
    }
}
