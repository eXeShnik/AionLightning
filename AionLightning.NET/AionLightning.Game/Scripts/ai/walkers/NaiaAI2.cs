// NaiaAI2 — Java ai/walkers/NaiaAI2.java. Naia walker NPC: shouts once when she comes into aggro
// range of two fixed waypoint NPCs (Cannon, Qydro), once per approach.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("naia")]
public sealed class NaiaAI2 : GeneralNpcAI2
{
    private bool _saidCannon;
    private bool _saidQydro;

    public override void OnMoveArrived()
    {
        base.OnMoveArrived();

        var cannon = GetNpc(203145);
        var qydro = GetNpc(203125);
        bool cannonNear = cannon is not null
            && Owner.Position.DistanceTo(cannon.Position) <= Owner.Template.AggroRange;
        bool qydroNear = qydro is not null
            && Owner.Position.DistanceTo(qydro.Position) <= Owner.Template.AggroRange;

        if (!_saidCannon && cannonNear)
        {
            _saidCannon = true;
            // note: Java looked up a "2"-param WALK_WAYPOINT shout via DataManager.NPC_SHOUT_DATA and
            // played it via NpcShoutsService after a 10s delay — NpcShoutData has no WALK_WAYPOINT event
            // type or per-shout param, and NpcShoutsService isn't reachable from the script layer.
        }
        else if (_saidCannon && !cannonNear)
        {
            _saidCannon = false;
        }

        if (!_saidQydro && qydroNear)
        {
            _saidQydro = true;
            // note: same WALK_WAYPOINT shout gap as above (param "1", no delay).
        }
        else if (_saidQydro && !qydroNear)
        {
            _saidQydro = false;
        }
    }
}
