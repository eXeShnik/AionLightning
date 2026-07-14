// FallenReianAI2 — Java ai/instance/rentusBase/FallenReianAI2.java. Opens the matching collapsed-
// building door once a player walks close enough.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("fallen_reian")]
public sealed class FallenReianAI2 : NpcAi2
{
    private int _doorId;

    public override void OnSpawned()
    {
        base.OnSpawned();
        _doorId = Owner.Template.NpcId == 799661 ? 16 : 54;
    }

    public override void OnCreatureMoved(Creature creature)
    {
        if (creature is not Player player) return;
        if (Owner.Position.DistanceTo(player.Position) > _doorId) return;
        // note: Java then checked the player's distance to instance door `_doorId` (<=30) and opened it
        // once (guarded by an isCollapsed flag) — instance doors aren't exposed to scripts yet.
    }
}
