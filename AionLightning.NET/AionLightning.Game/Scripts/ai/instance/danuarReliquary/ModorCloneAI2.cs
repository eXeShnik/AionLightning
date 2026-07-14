// ModorCloneAI2 — Java ai/instance/danuarReliquary/ModorCloneAI2.java. Clone add: 30s after first
// being attacked, casts Vengeful Orb and spawns the real Sorcerer Queen Modor.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("modorclone")]
public sealed class ModorCloneAI2 : AggressiveNpcAI2
{
    private bool _isHome = true;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (_isHome)
        {
            _isHome = false;
            StartSkillTask();
        }
    }

    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(() => SendMsg(1500746), 5000);
    }

    private void StartSkillTask()
    {
        ScheduleTask(() =>
        {
            if (Owner.IsAlreadyDead) return;
            // note: Java also cast skill 21177 lvl65 (Vengeful Orb) at the current (player) target
            // here; targeting a creature other than the owner isn't exposed via the UseSkill helper.
            UseSkill(21177, 65);
            ScheduleTask(SpawnSorcererQueenModor, 11000);
        }, 30000);
    }

    private void SpawnSorcererQueenModor()
    {
        if (Owner.IsAlreadyDead) return;
        Spawn(284443, 256.4457f, 257.6867f, 242.30f, 90);
        // note: Java force-killed the owner with a lethal self-hit (Creature.getController()
        // .onAttack); approximated here by zeroing CurrentHp — reward/loot/AI-transition side effects
        // of that hit aren't wired through this direct set.
        Owner.CurrentHp = 0;
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
        CancelTasks();
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        CancelTasks();
        _isHome = true;
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java also called AI2Actions.deleteOwner(this) here; no scripted despawn API exists yet.
    }
}
