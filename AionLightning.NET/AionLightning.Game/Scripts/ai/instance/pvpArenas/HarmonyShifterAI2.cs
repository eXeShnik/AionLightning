using System.Linq;
using System.Collections.Generic;
using System;
// HarmonyShifterAI2 — Java ai/instance/pvpArenas/HarmonyShifterAI2.java. Harmony-arena shifter:
// once used, buffs the arena's bonus-skill turrets, then respawns and deletes itself.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("harmony_shifter")]
public sealed class HarmonyShifterAI2 : ShifterAI2
{
    private bool _isRewarded;

    protected override void HandleUseItemFinish(Player player)
    {
        base.HandleUseItemFinish(player);
        if (_isRewarded) return;
        _isRewarded = true;
        // note: Java called AI2Actions.handleUseItemFinish(this, player), then looked up this turret's
        // paired bonus-skill turret(s) (207118/207119 for npc 207116, or 207100 for npc 207099) via
        // WorldMapInstance.getNpcs(id) and cast each one's HarmonyArenaReward.getNpcBonusSkill via
        // SkillEngine, before scheduling a respawn (AI2Actions.scheduleRespawn) and deleting itself
        // (AI2Actions.deleteOwner). Bulk npc-id lookup, HarmonyArenaReward access, and AI2Actions aren't
        // exposed to scripts yet.
    }
}
