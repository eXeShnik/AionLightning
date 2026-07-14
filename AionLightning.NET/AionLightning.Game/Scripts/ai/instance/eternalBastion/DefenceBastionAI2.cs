using System.Linq;
using System.Collections.Generic;
using System;
// DefenceBastionAI2 — Java ai/instance/eternalBastion/DefenceBastionAI2.java. Eternal Bastion entry
// NPC: offers a quest-gated dialog and, on entry-item consumption, teleports the player into a
// defence-bastion buff and deletes itself.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("defence_bastion")]
public sealed class DefenceBastionAI2 : GeneralNpcAI2
{
    protected int rewardDialogId = 5;
    protected int startingDialogId = 10;
    protected int questDialogId = 10;

    public override void OnDialogStart(Player player)
    {
        // note: Java resolved which dialog page to show (reward/quest/start/none) via QuestEngine.
        // getQuestNpc/player.getQuestStateList()/QuestService.checkStartConditions, then sent an
        // SM_DIALOG_WINDOW; also (via the dropped onDialogSelect hook) consumed entry item 185000136,
        // applied a race-specific buff (skill 21139/21138) through SkillEngine.applyEffectDirectly, and
        // deleted itself via AI2Actions.deleteOwner. QuestEngine, PacketSendUtility/SM_DIALOG_WINDOW, and
        // AI2Actions aren't wired at the script layer yet.
    }
}
