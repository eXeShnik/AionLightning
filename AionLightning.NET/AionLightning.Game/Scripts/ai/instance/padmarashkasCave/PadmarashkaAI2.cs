// PadmarashkaAI2 — Java ai/instance/padmarashkasCave/PadmarashkaAI2.java. Padmarashka boss: sleeps
// until first attacked/aggroed, then runs a staged encounter (entrance adds, egg spawns, cave-crumble
// rock spawns) gated by HP thresholds.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("padmarashka")]
public sealed class PadmarashkaAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java cast a self-sleep skill (19186) and set an abnormal sleep state on spawn (canThink()
        // == false until woken, no C# equivalent hook); skill casting and abnormal-state effects aren't
        // wired at the script layer yet.
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: Java woke on first attack/aggro (if not already awake), then ran a staged encounter: a 60s
        // entrance-adds spawn loop, a 60s stage-1 add + egg-guardian spawn loop, a 120s stage-2 huge-egg
        // spawn loop, and a stage-3 cave-crumble rock spawn triggered at 25% HP with a shout (1401215); all
        // driven by ThreadPoolManager/SpawnEngine/SkillEngine — none of that choreography is wired at the
        // script layer yet.
    }

    public override void OnCreatureAggro(Creature creature)
    {
        // note: same wake-on-aggro path as OnAttack.
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        // note: Java cancelled the encounter task, despawned every stage's spawned npcs, re-slept, and
        // spawned its "Padma Protector" guards.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java cancelled the encounter task and despawned every stage's spawned npcs.
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
        // note: same cleanup as OnDied.
    }
}
