// ReianGuardianStatueAI2 — Java ai/instance/kamarBattlefield/ReianGuardianStatueAI2.java.
// Rescuable-statue prop, gated on the instance having started progress, that rewards the player once.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("reianguardianstatue")]
public sealed class ReianGuardianStatueAI2 : ActionItemNpcAI2
{
    private bool _isRewarded;

    public override void OnDialogStart(Player player)
    {
        // note: Java bailed out here if the instance's InstanceReward hadn't started progress yet;
        // InstanceReward/WorldMapInstance lookups aren't exposed to scripts yet.
        base.OnDialogStart(player);
    }

    protected override void HandleUseItemFinish(Player player)
    {
        if (_isRewarded) return;
        _isRewarded = true;
        base.HandleUseItemFinish(player);
        // note: Java also deleted itself via AI2Actions.deleteOwner, which isn't exposed to scripts yet.
    }
}
