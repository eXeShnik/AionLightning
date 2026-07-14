// YamenesAI2 — Java ai/instance/abyssal_splinter/YamenesAI2.java. Abyssal Splinter final boss:
// alternates between two fixed portal layouts, periodically spawning 3 portal props and 3 adds
// near itself once engaged.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("yamennes")]
public sealed class YamenesAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java reset its percentage-threshold list, its portal-layout flip flag, and shouted
        // (1400732). NpcShoutsService isn't exposed to scripts yet.
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: Java started two tasks on first attack: a one-shot 10-minute enrage skill (19098), and a
        // 60s-repeating cycle that spawned 3 portal props (282014/282015/282131) at one of two fixed
        // layouts (flipping each cycle) unless portals already existed, then after 3s deleted any active
        // adds (282107), cast a skill (19282) on its current target, spawned 3 new adds around itself,
        // reset its attacked-count, and shouted. Instance npc lookup/deletion and NpcShoutsService aren't
        // exposed to scripts yet.
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        // note: Java reset its threshold list and portal-layout flag, and cancelled the repeating task.
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
        // note: Java cleared its threshold list, cancelled the repeating task, and deleted any active
        // adds (282107).
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: same threshold/task/add cleanup as OnDespawned.
    }
}
