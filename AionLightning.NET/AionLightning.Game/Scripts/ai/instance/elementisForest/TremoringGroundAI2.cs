using System.Linq;
using System.Collections.Generic;
using System;
// TremoringGroundAI2 — Java ai/instance/elementisForest/TremoringGroundAI2.java. Trap npc: once a
// player gets within 16m, casts a debuff on them 2s later (one-shot).
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("tremorground")]
public sealed class TremoringGroundAI2 : GeneralNpcAI2
{
    private bool _isUsed;

    // note: Java also overrode ask(CAN_ATTACK_PLAYER=POSITIVE); that hook doesn't exist on NpcAi2.
    public override void OnCreatureMoved(Creature creature)
    {
        if (creature is not Player player) return;
        if (Owner.Position.DistanceTo(player.Position) > 16) return;
        if (_isUsed) return;

        _isUsed = true;
        ScheduleTask(() =>
        {
            Owner.Target = player;
            UseSkill(19442);
            // note: Java then deleted itself via AI2Actions.deleteOwner; no scripted despawn API exists yet.
        }, 2000);
    }
}
