using System;
// BattleRationsAI2 — Java ai/instance/steelRose/BattleRationsAI2.java. Battle Rations use-item:
// heals the player and self-deletes.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("battle_rations")]
public sealed class BattleRationsAI2 : ActionItemNpcAI2
{
    private const int BattleRationsNpcId = 730770;
    private const int HealAmount = 5000;

    protected override void HandleUseItemFinish(Player player)
    {
        if (Owner.Template.NpcId == BattleRationsNpcId)
        {
            player.CurrentHp = Math.Min(player.MaxHp, player.CurrentHp + HealAmount);
            // note: Java also called AI2Actions.deleteOwner(this); no scripted despawn API exists yet.
        }
    }
}
