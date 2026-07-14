// CadellaAI2 — Java ai/instance/argentManor/CadellaAI2.java. Argent Manor boss: on first attack
// spawns 5 hetgolem helpers and starts a repeating heal-phase cycle; opens instance doors on death.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("cadella")]
public sealed class CadellaAI2 : AggressiveNpcAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: Java spawned 5 hetgolem helpers (282345-282349) around itself on first attack, then
        // started a repeating 60s phase task: cast a no-animation heal skill (19541), gate think() off
        // via canThink (no C# equivalent), have a random surviving helper "attack" it back after 2s, then
        // resume thinking and cycle to the next event skill id (starting at 19533) after 17s. All of this
        // depends on WorldMapInstance NPC lookup, EmoteManager, AggroList and canThink — none ported yet.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java cancelled the phase task, deleted the spawned hetgolem helpers, and opened instance
        // doors 15/64/76; WorldMapInstance door control isn't exposed to scripts yet.
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
        // note: same phase-task cancel + helper cleanup as OnDied.
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        // note: Java removed 13 tracked skill effects, reset the event-skill cursor to 19533, cancelled
        // the phase task and deleted helpers; EffectController isn't exposed to scripts yet.
    }
}
