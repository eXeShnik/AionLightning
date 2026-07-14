using System.Linq;
using System.Collections.Generic;
using System;
// RelicsAI2 — Java ai/instance/pvpArenas/RelicsAI2.java. PvP-arena relic: usable only once the
// instance's reward progress has started; using it deletes the relic, respawning it unless it's one
// of the two capture-point relics.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("pvparenarelics")]
public sealed class RelicsAI2 : ActionItemNpcAI2
{
    private bool _isRewarded;

    public override void OnDialogStart(Player player)
    {
        // note: Java gated the use-item dialog on
        // getPosition().getWorldMapInstance().getInstanceHandler().getInstanceReward().isStartProgress();
        // instance-reward progress state isn't exposed to scripts yet, so the gate always passes.
        base.OnDialogStart(player);
    }

    protected override void HandleUseItemFinish(Player player)
    {
        if (_isRewarded) return;
        _isRewarded = true;
        // note: Java called AI2Actions.handleUseItemFinish(this, player), scheduled a respawn
        // (AI2Actions.scheduleRespawn) unless this relic's npc id was 701187/701188, then deleted itself
        // (AI2Actions.deleteOwner). AI2Actions isn't wired at the script layer yet.
    }
}
