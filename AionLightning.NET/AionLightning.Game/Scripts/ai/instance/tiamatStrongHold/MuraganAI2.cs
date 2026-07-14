// MuraganAI2 — Java ai/instance/tiamatStrongHold/MuraganAI2.java. Story NPC chain: shouts on
// spawn, then walks to a fixed point and advances a race-specific quest once a player draws near.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("muragan")]
public sealed class MuraganAI2 : GeneralNpcAI2
{
    private bool _isMove;

    public override void OnSpawned()
    {
        base.OnSpawned();
        if (Owner.Template.NpcId == 800438)
        {
            SendMsg(390852);
        }
    }

    public override void OnCreatureSee(Creature creature) => CheckDistance(creature);

    public override void OnCreatureMoved(Creature creature) => CheckDistance(creature);

    private void CheckDistance(Creature creature)
    {
        if (creature is Player player && Owner.Position.DistanceTo(creature.Position) <= 15 && !_isMove)
        {
            _isMove = true;
            OpenSuramaDoor();
            StartWalk(player);
        }
    }

    private void StartWalk(Player player)
    {
        int owner = Owner.Template.NpcId;
        if (owner is 800436 or 800438) return;

        if (owner == 800435)
        {
            SendMsg(390837);
            SendMsg(390838);
            KillGuardCaptain();
        }
        // note: Java walked the owner to map point (838, 1317, 396) via MoveController/WalkManager and
        // broadcast a START_EMOTE2 SM_EMOTION — movement/emote plumbing isn't exposed to scripts yet.
        ScheduleTask(() =>
        {
            ForQuest(player);
            // note: Java also deleted this owner (AI2Actions.deleteOwner) — no scripted NPC-removal
            // API exists yet.
        }, 10000);
    }

    private void OpenSuramaDoor()
    {
        if (Owner.Template.NpcId == 800436)
        {
            SendMsg(390835);
            // note: Java opened instance door 56 and deleted this owner here — instance doors and
            // NPC removal aren't exposed to scripts yet.
        }
    }

    private void KillGuardCaptain()
    {
        // note: Java scanned the current instance for NPC 219440, spawned replacement NPC 283448 in its
        // place, and removed the original. Bulk instance-NPC enumeration and NPC removal aren't exposed
        // to scripts yet.
    }

    private void ForQuest(Player player)
    {
        // note: Java advanced quest 30708 (Elyos) / 30758 (Asmodian) var 0 by one (capped at 5) and sent
        // an SM_QUEST_ACTION update. Quest state access isn't exposed to scripts yet.
    }
}
