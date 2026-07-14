using System.Linq;
using System.Collections.Generic;
using System;
// JurdinsIllusionAI2 — Java ai/instance/elementisForest/JurdinsIllusionAI2.java. Illusion npc:
// starting a dialog swaps it for the real Jurdin a few seconds later.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("jurdins_illusion")]
public sealed class JurdinsIllusionAI2 : GeneralNpcAI2
{
    private bool _isSpawned;

    public override void OnDialogStart(Player player)
    {
        if (!_isSpawned)
        {
            _isSpawned = true;
            ScheduleTask(() =>
            {
                ScheduleTask(() =>
                {
                    // note: Java spawned this at the illusion's original world/instance explicitly via
                    // SpawnEngine; Spawn() always targets the owner's current world/instance scope, which
                    // matches here since the illusion never changes instance.
                    Spawn(217238, 472.989f, 798.109f, 130.072f, 90);
                    Spawn(282465, 472.989f, 798.109f, 130.072f);
                    // note: Java deleted the smoke npc (282465) via NpcActions.delete immediately; no
                    // scripted despawn API exists yet.
                }, 4000);
                // note: Java then deleted itself via AI2Actions.deleteOwner; no scripted despawn API
                // exists yet.
            }, 3000);
        }
        base.OnDialogStart(player);
    }
}
