using System.Linq;
using System.Collections.Generic;
using System;
// CelestiusAI2 — Java ai/instance/tallocsHollow/CelestiusAI2.java. Talloc's Hollow boss: on first
// engagement, periodically clears and re-spawns three walking helper adds.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("celestius")]
public sealed class CelestiusAI2 : AggressiveNpcAI2
{
    private bool _isHome = true;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (_isHome)
        {
            _isHome = false;
            StartHelpersCall();
        }
    }

    private void StartHelpersCall()
    {
        ScheduleTask(() =>
        {
            // note: Java guarded each tick with isAlreadyDead()/cancelHelpersTask(); CancelTasks() already
            // stops this loop from the base OnDied() hook, so the guard is redundant here.
            DeleteHelpers();
            UseSkill(18981);
            StartRun(Spawn(281514, 518, 813, 1378));
            StartRun(Spawn(281514, 551, 795, 1376));
            StartRun(Spawn(281514, 574, 854, 1375));
        }, 1000, 25000);
    }

    private void StartRun(Npc? npc)
    {
        // note: Java assigned a walker route id (npc.getSpawn().setWalkerId), started WalkManager
        // walking, set the NPC's client state, and broadcast a START_EMOTE2 packet; walker-route
        // assignment/WalkManager/state-broadcast aren't exposed to scripts yet.
    }

    private void DeleteHelpers()
    {
        // note: Java found every live helper (281514) at this boss's three spawn spots via
        // WorldMapInstance.getNpcs(281514) and deleted them; bulk npc-id lookup and scripted despawn
        // aren't exposed to scripts yet.
    }

    public override void OnBackHome()
    {
        CancelTasks();
        DeleteHelpers();
        _isHome = true;
        base.OnBackHome();
    }

    public override void OnDespawned()
    {
        CancelTasks();
        DeleteHelpers();
        base.OnDespawned();
    }

    public override void OnDied()
    {
        DeleteHelpers();
        base.OnDied();
    }
}
