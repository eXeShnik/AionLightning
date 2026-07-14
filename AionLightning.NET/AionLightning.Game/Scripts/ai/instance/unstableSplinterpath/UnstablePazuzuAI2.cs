using System.Linq;
using System.Collections.Generic;
using System;
// UnstablePazuzuAI2 — Java ai/instance/unstableSplinterpath/UnstablePazuzuAI2.java. Boss: on first
// attack shouts and starts a repeating skill/worm-add spawn task.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("unstablepazuzu")]
public sealed class UnstablePazuzuAI2 : AggressiveNpcAI2
{
    private bool _isHome = true;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (_isHome)
        {
            _isHome = false;
            SendMsg(342219);
            StartTask();
        }
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
        SendMsg(1500003);
    }

    private void StartTask()
    {
        ScheduleTask(() =>
        {
            UseSkill(19145);
            if (GetNpc(219570) is null)
            {
                Spawn(219570, 651.351990f, 326.425995f, 465.523987f, 8);
                Spawn(219570, 666.604980f, 314.497009f, 465.394012f, 27);
                Spawn(219570, 685.588989f, 342.955994f, 465.908997f, 68);
                Spawn(219570, 651.322021f, 346.554993f, 465.563995f, 111);
            }
        }, 5000, 70000);
    }
}
