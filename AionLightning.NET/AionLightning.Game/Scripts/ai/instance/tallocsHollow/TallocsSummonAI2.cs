using System.Linq;
using System.Collections.Generic;
using System;
// TallocsSummonAI2 — Java ai/instance/tallocsHollow/TallocsSummonAI2.java. Offers a transform-into-
// summon dialog; once accepted, the player rides this NPC as a controllable summon.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("tallocssummon")]
public sealed class TallocsSummonAI2 : NpcAi2
{
    private bool _isTransformed;

    // note: Java's onDialogSelect(player, dialogId, questId, extendedRewardIndex) — the hook this NPC
    // used to build a Summon from itself (new Summon(...), player.setSummon, SM_TRANSFORM_IN_SUMMON,
    // SM_CUSTOM_SETTINGS, SM_EMOTION) on dialogId 59 and flip _isTransformed to true — has no C#
    // equivalent hook, and Summon construction/transform packets aren't wired at the script layer yet.
    // _isTransformed is therefore kept for structural fidelity but never becomes true.

    public override void OnDialogStart(Player player)
    {
        if (!_isTransformed)
        {
            // note: Java sent an SM_DIALOG_WINDOW(getObjectId(), 10) here; PacketSendUtility/
            // SM_DIALOG_WINDOW aren't wired at the script layer yet.
        }
    }
}
