using System.Linq;
using System.Collections.Generic;
using System;
// IdeResonatorAI2 — Java ai/instance/infinityShard/IdeResonatorAI2.java. Infinity Shard resonator:
// periodically targets and casts its power skill onto the Hyperion boss.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("ideres")]
public sealed class IdeResonatorAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        StartPower();
    }

    private void StartPower()
    {
        ScheduleTask(() =>
        {
            // note: Java re-targeted the Hyperion boss (npc 231073) via AI2Actions.targetCreature before
            // each cast; target reassignment isn't exposed to scripts yet, so the skill below fires
            // against this resonator's own current target instead.
            UseSkill(21257);
        }, 3000, 5000);
    }

    public override void OnDied()
    {
        base.OnDied();
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
    }

    // note: Java overrode pollInstance to refuse SHOULD_DECAY/SHOULD_RESPAWN/SHOULD_REWARD — no C#
    // equivalent poll exists.
}
