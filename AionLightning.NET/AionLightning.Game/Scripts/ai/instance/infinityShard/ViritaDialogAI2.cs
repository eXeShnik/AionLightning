using System.Linq;
using System.Collections.Generic;
using System;
// ViritaDialogAI2 — Java ai/instance/infinityShard/ViritaDialogAI2.java. Vritra dialog NPC: shouts
// an intro sequence, then deletes itself and spawns the Hyperion encounter's opening add wave.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("vritragialog_giperion")]
public sealed class ViritaDialogAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java shouted 1500761/1500762/1500760 via NpcShoutsService.sendMsg at staggered delays
        // (10s/15s/20s); NPC shouts aren't exposed to scripts yet.
        StartEventTask();
    }

    private void StartEventTask()
    {
        ScheduleTask(() =>
        {
            if (Owner.IsAlreadyDead) return;
            // note: Java deleted itself via AI2Actions.deleteOwner here; no scripted despawn API exists
            // yet.
            Spawn(230396, 109.6370f, 140.9392f, 112.1742f, 12);
            Spawn(230396, 130.4754f, 132.7612f, 112.2131f, 83);
            Spawn(230396, 151.7122f, 132.7612f, 112.2131f, 83);
            Spawn(230396, 124.9110f, 162.9065f, 129.2247f, 64);

            Spawn(230397, 106.8842f, 143.6426f, 112.2893f, 24);
            Spawn(230397, 133.4831f, 113.4827f, 128.9372f, 10);
            Spawn(230397, 147.9318f, 136.2378f, 112.1742f, 60);
            Spawn(230397, 127.3380f, 161.2330f, 129.2247f, 92);
        }, 30000);
    }
}
