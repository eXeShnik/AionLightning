using System.Linq;
using System.Collections.Generic;
using System;
// PlazaFlameThrowerAI2 — Java ai/instance/pvpArenas/PlazaFlameThrowerAI2.java. PvP-arena flame
// thrower: usable only once the instance's reward progress has started; using it buffs the arena's
// bonus-skill turrets, then respawns and deletes itself.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("plaza_flame_thrower")]
public sealed class PlazaFlameThrowerAI2 : ShifterAI2
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
        base.HandleUseItemFinish(player);
        if (_isRewarded) return;
        _isRewarded = true;
        // note: Java called AI2Actions.handleUseItemFinish(this, player), then looked up this thrower's
        // paired bonus-skill turret pairs (keyed by this npc's id: 701169/701170/701171/701172) via
        // WorldMapInstance.getNpcs(id) and cast each one's PvPArenaReward.getNpcBonusSkill via
        // SkillEngine, before scheduling a respawn (AI2Actions.scheduleRespawn) and deleting itself
        // (AI2Actions.deleteOwner). Bulk npc-id lookup, PvPArenaReward access, and AI2Actions aren't
        // exposed to scripts yet.
    }
}
