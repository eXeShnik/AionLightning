using System.Linq;
using System.Collections.Generic;
using System;
// UnstablePazuzuWormAI2 — Java ai/instance/unstableSplinterpath/UnstablePazuzuWormAI2.java. Worm
// add: targets Pazuzu's spawner and casts a skill on it 3s after spawning.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("unstablepazuzuworm")]
public sealed class UnstablePazuzuWormAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(() =>
        {
            Owner.Target = GetNpc(219554);
            UseSkill(19291);
        }, 3000);
    }
}
