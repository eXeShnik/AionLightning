using System.Linq;
using System.Collections.Generic;
using System;
// AntiAirCraftGunAI2 — Java ai/instance/pvpArenas/AntiAirCraftGunAI2.java. PvP-arena turret: usable
// only once the instance's reward progress has started; using it teleports and morphs the player.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("antiaircraftgun")]
public sealed class AntiAirCraftGunAI2 : ActionItemNpcAI2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java gated the use-item dialog on
        // getPosition().getWorldMapInstance().getInstanceHandler().getInstanceReward().isStartProgress();
        // instance-reward progress state isn't exposed to scripts yet, so the gate always passes.
        base.OnDialogStart(player);
    }

    protected override void HandleUseItemFinish(Player player)
    {
        int morphSkill = Owner.Template.NpcId switch
        {
            701185 or 701321 => 0x4E502E, // 20048 lvl 46
            701199 or 701322 => 0x4E5133, // 20049 lvl 51
            701213 or 701323 => 0x4E5238, // 20050 lvl 56
            _ => 0
        };
        // note: Java teleported the player to this turret's position (TeleportService2.teleportTo),
        // stopped their protection-active task, and cast morphSkill (id/level packed as morphSkill>>8 /
        // morphSkill&0xFF) on the player via SkillEngine; TeleportService/protection-task/player-targeted
        // no-animation casts aren't exposed to scripts yet. Java also called AI2Actions.scheduleRespawn
        // and AI2Actions.deleteOwner — neither is wired at the script layer yet.
    }
}
