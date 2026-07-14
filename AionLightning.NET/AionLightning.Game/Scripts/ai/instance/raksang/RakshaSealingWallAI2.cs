// RakshaSealingWallAI2 — Java ai/instance/raksang/RakshaSealingWallAI2.java. Seal prop: on player
// approach, spawns Raksha (or its guarded variant, depending on which prior bosses are down) and
// removes itself.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("raksha_sealing_wall")]
public sealed class RakshaSealingWallAI2 : GeneralNpcAI2
{
    private bool _startedEvent;

    // note: Java also overrode canThink to always return false — no C# equivalent hook exists.

    public override void OnCreatureMoved(Creature creature)
    {
        if (creature is not Player player) return;
        if (getOwner().Position.DistanceTo(player.Position) > 35) return;
        if (_startedEvent) return;
        _startedEvent = true;

        var sharik = GetNpc(217425);
        var flamelord = GetNpc(217451);
        var sealguard = GetNpc(217456);
        bool allDown = IsDeadOrGone(sharik) && IsDeadOrGone(flamelord) && IsDeadOrGone(sealguard);
        int bossId = allDown ? 217475 : 217647;
        Spawn(bossId, 1063.08f, 903.13f, 138.744f, 29);
        // note: Java deleted itself via AI2Actions.deleteOwner; no scripted despawn API exists yet.
    }

    private static bool IsDeadOrGone(Npc? npc) => npc is null || npc.IsAlreadyDead;
}
