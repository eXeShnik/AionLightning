// ReianSupplyAI2 — Java ai/instance/kamarBattlefield/ReianSupplyAI2.java. Rescuable-supply prop,
// gated on the instance having started progress, that rewards the player once then dies silently.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("reiansupply")]
public sealed class ReianSupplyAI2 : ActionItemNpcAI2
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
        // note: Java died silently via AI2Actions.dieSilently instead of deleting itself; AI2Actions
        // isn't exposed to scripts yet.
    }
}
