using System.Linq;
using System.Collections.Generic;
using System;
// KinquidAI2 — Java ai/instance/tallocsHollow/KinquidAI2.java. Talloc's Hollow boss: closes an
// instance door on aggro, periodically re-casts a self skill pair, and cycles a random destroyer
// add near itself.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("kinquid")]
public sealed class KinquidAI2 : AggressiveNpcAI2
{
    private bool _isHome = true;

    public override void OnCreatureAggro(Creature creature)
    {
        base.OnCreatureAggro(creature);
        if (_isHome)
        {
            _isHome = false;
            // note: Java closed instance door 48 via WorldMapInstance.getDoors(); instance-door state
            // isn't exposed to scripts yet.
            Check();
            StartSkillTask();
        }
    }

    public override void OnBackHome()
    {
        CancelTasks();
        _isHome = true;
        // note: Java re-opened instance door 48 via WorldMapInstance.getDoors(); instance-door state
        // isn't exposed to scripts yet.
        base.OnBackHome();
        DespawnDestroyer();
    }

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }

    public override void OnDied()
    {
        CancelTasks();
        base.OnDied();
    }

    private void StartSkillTask()
    {
        ScheduleTask(() =>
        {
            // note: Java guarded each tick with isAlreadyDead(); CancelTasks() already stops this loop
            // from the base OnDied() hook, so the guard is redundant here.
            UseSkill(19233);
            ScheduleTask(() => UseSkill(19234), 3500);
        }, 35000, 35000);
    }

    private void DespawnDestroyer()
    {
        // note: Java deleted any live 282008/282009 destroyer add via WorldMapInstance.getNpc +
        // getController().onDelete(); scripted NPC removal isn't exposed yet (GetNpc only reads).
    }

    private void Check()
    {
        DespawnDestroyer();
        // note: Java also gated this on getPosition().isSpawned(); world-presence isn't exposed to
        // scripts yet, so only the death/home checks below are enforced.
        if (!Owner.IsAlreadyDead && !_isHome)
        {
            int spawnId = Random.Shared.Next(1, 3) == 1 ? 282008 : 282009;
            switch (Random.Shared.Next(1, 4))
            {
                case 1:
                    Spawn(spawnId, 266.70685f, 680.6733f, 1167.2369f);
                    break;
                case 2:
                    Spawn(spawnId, 292.02466f, 719.7132f, 1169.3982f);
                    break;
                case 3:
                    Spawn(spawnId, 263.4334f, 716.73004f, 1170.3693f);
                    break;
            }
        }

        ScheduleTask(Check, 25000);
    }
}
