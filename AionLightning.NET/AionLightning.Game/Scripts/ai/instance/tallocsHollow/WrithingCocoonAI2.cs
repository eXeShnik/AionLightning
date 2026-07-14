using System.Linq;
using System.Collections.Generic;
using System;
// WrithingCocoonAI2 — Java ai/instance/tallocsHollow/WrithingCocoonAI2.java. Cocoon NPC that, once
// consumed with a keeper's whistle, deletes its paired cocoon and spawns a rideable summon.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("writhingcocoon")]
public sealed class WrithingCocoonAI2 : NpcAi2
{
    // note: Java's onDialogSelect(player, dialogId, questId, extendedRewardIndex) — the hook this NPC
    // used to consume item 185000088 on dialogId 1012, delete its paired cocoon (730232/730233),
    // spawn the matching summon (799500/799501), send a follow-up SM_SYSTEM_MESSAGE, and delete itself
    // via AI2Actions.deleteOwner — has no C# equivalent hook; item consumption, scripted NPC removal,
    // and AI2Actions aren't wired at the script layer yet.

    public override void OnDialogStart(Player player)
    {
        // note: Java sent an SM_DIALOG_WINDOW(getObjectId(), 1011) here; PacketSendUtility/
        // SM_DIALOG_WINDOW aren't wired at the script layer yet.
    }
}
