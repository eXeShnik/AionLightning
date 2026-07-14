using System.Linq;
using System.Collections.Generic;
using System;
// PashidCommanderAI2 — Java ai/instance/eternalBastion/PashidCommanderAI2.java. Casts a legion-bless
// then a self-buff shortly after spawning.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("pashid_commander")]
public sealed class PashidCommanderAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        PashidSkill(3000);
        PashidSkill(6000);
        base.OnSpawned();
    }

    private void PashidFirstSkill(int skillId) => UseSkill(skillId);

    private void VritraLegionBless(int skillId) => UseSkill(skillId);

    private void PashidSkill(int time)
    {
        ScheduleTask(() =>
        {
            if (time == 3000)
                VritraLegionBless(20700);
            if (time == 6000)
                PashidFirstSkill(21237);
        }, time);
    }

    protected int GetTalkDelay() => 0; // note: NpcObjectTemplate talk-delay isn't exposed on NpcTemplate yet.
}
