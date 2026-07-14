using System.Linq;
using System.Collections.Generic;
using System;
// TrappedTermaAI2 — Java ai/instance/elementisForest/TrappedTermaAI2.java. On first attack,
// staggers toward a fixed point and force-dies 16s later; on death frees the real Terma.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("terma")]
public sealed class TrappedTermaAI2 : NpcAi2
{
    private bool _isMove;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (!_isMove)
        {
            _isMove = true;
            MoveToDead();
        }
    }

    public override void OnDied()
    {
        base.OnDied();
        Spawn(205495, 455.94f, 537.06f, 132.6f);
        Spawn(701009, 451.706f, 534.313f, 131.979f);
        // note: Java captured the newly-spawned freeTerma and made it shout (1500444) via
        // NpcShoutsService; SendMsg only covers a shout from this AI's own owner, not from a different
        // npc, so it isn't reproduced here.
    }

    private void MoveToDead()
    {
        // note: Java set state WALKING via WalkManager.startWalking, moved to a fixed point
        // (455.93f, 537.2f, 132.55f), and broadcast an SM_EMOTION(START_EMOTE2); WalkManager/
        // MoveController/PacketSendUtility aren't exposed to scripts yet. It also overrode modifyDamage
        // to always return 1 (near-invulnerable) and ask/pollInstance (SHOULD_DECAY/RESPAWN/REWARD=
        // NEGATIVE) — none of those hooks exist on NpcAi2.
        Dead();
    }

    private void Dead()
    {
        ScheduleTask(() =>
        {
            // note: Java called getController().die() here to force-kill the owner after this 16s
            // stagger; no scripted force-death API exists yet.
        }, 16000);
    }
}
