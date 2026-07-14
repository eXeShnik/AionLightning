using System.Linq;
using System.Collections.Generic;
using System;
// UnstableKaluvaAI2 — Java ai/instance/unstableSplinterpath/UnstableKaluvaAI2.java. Boss: rarely
// moves to an egg spawner mid-fight to hatch it, then resumes combat once the interaction ends.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("unstablekaluva")]
public sealed class UnstableKaluvaAI2 : AggressiveNpcAI2
{
    private bool _canThink = true;
    private int _egg;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (Random.Shared.Next(0, 101) < 3)
            MoveToSpawner();
    }

    private void MoveToSpawner()
    {
        RandomEgg();
        var spawner = GetNpc(_egg);
        if (spawner is not null)
        {
            UseSkill(19152);
            _canThink = false;
            // note: Java also stopped its attack emote (EmoteManager), set state FOLLOWING, broadcast an
            // SM_EMOTION(START_EMOTE2), targeted the spawner (AI2Actions.targetCreature) and moved to it
            // (MoveController.moveToTargetObject) — none of that is exposed to scripts, so OnMoveArrived
            // below never actually fires from this path.
        }
    }

    public override void OnMoveArrived()
    {
        if (!_canThink)
        {
            var spawner = GetNpc(_egg);
            if (spawner is not null)
            {
                spawner.RemoveEffectBySkillId(19222);
                // note: Java cast this as the owner targeting `spawner`; UseSkill ignores the target.
                UseSkill(19223);
                Owner.RemoveEffectBySkillId(19152);
            }

            ScheduleTask(() =>
            {
                _canThink = true;
                // note: Java re-picked the most-hated aggro target here (AggroList.getMostHated) and
                // forced AiState.FIGHT plus a think() tick; aggro-list access and forced state
                // transitions aren't exposed to scripts — NpcAiService owns that loop today.
            }, 2000);
        }
        base.OnMoveArrived();
    }

    private void RandomEgg()
    {
        switch (Random.Shared.Next(1, 5))
        {
            case 1: _egg = 219583; break;
            case 2: _egg = 219582; break;
            case 3: _egg = 219564; break;
            case 4: _egg = 219581; break;
        }
    }
}
