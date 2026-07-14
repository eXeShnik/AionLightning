// AncientRunishCodexAI2 — Java ai/instance/danuarMysticarium/AncientRunishCodexAI2.java. Use-item
// codex that grants a one-time instance reward.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("ancient_runish_codex")]
public sealed class AncientRunishCodexAI2 : ActionItemNpcAI2
{
    private bool _isRewarded;

    public override void OnDialogStart(Player player)
    {
        // note: Java bailed out here unless the owning instance's InstanceReward had already started
        // progress (WorldMapInstance.getInstanceHandler().getInstanceReward()); InstanceReward isn't
        // exposed to the script layer yet, so the gate is skipped.
        base.OnDialogStart(player);
    }

    protected override void HandleUseItemFinish(Player player)
    {
        if (_isRewarded) return;
        _isRewarded = true;
        // note: Java delegated to AI2Actions.handleUseItemFinish(this, player), scheduled a respawn
        // for non-boss npcIds (831145/831146/831147 excluded), then deleted the owner (AI2Actions
        // .deleteOwner). AI2Actions has no ported equivalent.
    }
}
