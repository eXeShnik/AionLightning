using System.Linq;
using System.Collections.Generic;
using System;
// TankBastionAI2 — Java ai/instance/eternalBastion/TankBastionAI2.java. Eternal Bastion tank-morph
// NPC: offers a quest-gated dialog and, on entry-item consumption, applies a tank-ride buff and
// deletes itself.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("tank_bastion")]
public sealed class TankBastionAI2 : GeneralNpcAI2
{
    protected int rewardDialogId = 5;
    protected int startingDialogId = 10;
    protected int questDialogId = 10;

    public override void OnDialogStart(Player player)
    {
        // note: Java resolved which dialog page to show (reward/quest/start/none) via QuestEngine.
        // getQuestNpc/player.getQuestStateList()/QuestService.checkStartConditions, then sent an
        // SM_DIALOG_WINDOW; also (via the dropped onDialogSelect hook) consumed entry item 185000137,
        // applied the tank-ride buff (skill 21141) through SkillEngine.applyEffectDirectly, and deleted
        // itself via AI2Actions.deleteOwner. QuestEngine, PacketSendUtility/SM_DIALOG_WINDOW, and
        // AI2Actions aren't wired at the script layer yet.
    }
}
