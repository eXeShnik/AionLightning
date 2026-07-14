// RackAI2 — Java ai/instance/kamarBattlefield/RackAI2.java. Usable food-rack prop that grants a
// randomized item count on use.
using System;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("rack")]
public sealed class RackAI2 : ActionItemNpcAI2
{
    private readonly int _rackCount = Random.Shared.Next(1, 11);

    protected override void HandleUseItemFinish(Player player)
    {
        if (getOwner().Template.NpcId == 801777)
        {
            // note: Java granted _rackCount (1-10) of item 162000134 via ItemService.addItem, then deleted
            // itself via AI2Actions.deleteOwner; ItemService/AI2Actions aren't exposed to scripts yet.
        }
    }
}
