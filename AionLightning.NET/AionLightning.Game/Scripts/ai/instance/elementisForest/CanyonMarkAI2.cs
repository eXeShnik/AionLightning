using System.Linq;
using System.Collections.Generic;
using System;
// CanyonMarkAI2 — Java ai/instance/elementisForest/CanyonMarkAI2.java. Marks its target 5s after
// spawn, then finishes the mark with a second skill 5-10s later.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("canyonmark")]
public sealed class CanyonMarkAI2 : AggressiveNpcAI2
{
    private Creature? _target;

    public override void OnSpawned()
    {
        base.OnSpawned();
        MarkTarget();
    }

    private void MarkTarget()
    {
        ScheduleTask(() =>
        {
            _target = Owner.Target as Creature;
            if (_target is not null)
            {
                UseSkill(19504);
                ScheduleTask(() =>
                {
                    if (!Owner.IsAlreadyDead)
                    {
                        Owner.Target = _target;
                        UseSkill(19505);
                        // note: Java then deleted itself via AI2Actions.deleteOwner; no scripted despawn
                        // API exists yet.
                    }
                }, Random.Shared.Next(5, 11) * 1000);
            }
            // note: when no target was found, Java deleted itself via AI2Actions.deleteOwner; same gap.
        }, 5000);
    }
}
