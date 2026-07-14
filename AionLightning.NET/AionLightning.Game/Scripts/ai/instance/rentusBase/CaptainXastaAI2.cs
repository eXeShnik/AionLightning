// CaptainXastaAI2 — Java ai/instance/rentusBase/CaptainXastaAI2.java. Rentus Base boss shared by two
// npc ids: 217309 runs a walker-route "phase" loop with helper npcs, 217310 (post-revive) casts a
// periodic nova skill instead.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("captain_xasta")]
public sealed class CaptainXastaAI2 : AggressiveNpcAI2
{
    private bool _isHome = true;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (!_isHome) return;
        _isHome = false;
        if (Owner.Template.NpcId == 217309)
        {
            SendMsg(1500388);
            // note: Java started a repeating 28s walker-route phase here (stop-attacking emote, walker
            // route, spawn 2 helper npcs 282604 with their own walker routes, then a 23s "sanctuary" timer
            // that either resumes combat or re-targets the most-hated attacker) — WalkManager/AggroList/
            // PacketSendUtility have no equivalent at the script layer.
        }
        else
        {
            ScheduleTask(() =>
            {
                if (Owner.IsAlreadyDead) return;
                UseSkill(19729);
                SendMsg(1500392);
            }, 30000, 30000);
        }
    }

    public override void OnMoveArrived()
    {
        base.OnMoveArrived();
        // note: Java stopped the walker route here once the walker template id was cleared — no walker
        // service is exposed to scripts yet.
    }

    public override void OnDied()
    {
        base.OnDied();
        if (Owner.Template.NpcId == 217309)
        {
            SendMsg(1500390);
            Spawn(217310, 238.160f, 598.624f, 178.480f);
            // note: Java also deleted the helper npcs (282604) and called AI2Actions.deleteOwner(this) —
            // NPC listing/delete isn't exposed to scripts yet.
        }
        else
        {
            SendMsg(1500391);
            // note: Java then revived "ariana" (799668) — removing her sanctuary effect (19921),
            // restarting her walker route, shouting two lines, casting skill 19358, opening instance door
            // 145, deleting the surrounding npcs (701156), and finally deleting ariana herself after
            // another delay. WorldMapInstance/WalkManager/NPC-delete access aren't exposed to scripts yet.
        }
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
    }

    public override void OnBackHome()
    {
        _isHome = true;
        base.OnBackHome();
        // note: Java also deleted the helper npcs (282604) here.
    }
}
