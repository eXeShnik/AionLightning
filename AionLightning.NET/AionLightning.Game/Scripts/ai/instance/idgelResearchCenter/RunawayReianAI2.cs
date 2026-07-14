// RunawayReianAI2 — Java ai/instance/idgelResearchCenter/RunawayReianAI2.java. Rescue-and-reward
// prop, gated on the instance having started progress.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("runaway_reian")]
public sealed class RunawayReianAI2 : ActionItemNpcAI2
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
        // note: Java's npcId == 800568 branch was empty; also deleted itself via AI2Actions.deleteOwner,
        // which isn't exposed to scripts yet.
    }
}
