// Door2AI2 — Java ai/instance/beshmundirTemple/Door2AI2.java. Instance door gated on the
// race-specific group quest ("The Rod and the Orb" / "A Quartz Is a Quartz") being active.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;

namespace Ai;

[AiName("door2")]
public sealed class Door2AI2 : ActionItemNpcAI2
{
    private const int ElyosQuestId = 30211;    // [Group] The Rod and the Orb
    private const int AsmodianQuestId = 30311; // [Group] A Quartz Is a Quartz

    public override void OnDialogStart(Player player)
    {
        // note: Java's rejection branch sent a chat message + an SM_DIALOG_WINDOW; neither is wired at
        // the script layer yet, so failing the quest gate below has no observable effect here.
        var questId = player.Race == Race.ELYOS ? ElyosQuestId : AsmodianQuestId;
        var entry = player.Quests.Get(questId);
        if (entry is not null && entry.Status != QuestStatus.NONE)
        {
            base.OnDialogStart(player);
        }
    }

    protected override void HandleUseItemFinish(Player player)
    {
        // note: Java deleted the door via AI2Actions.deleteOwner; no scripted despawn API exists yet.
    }
}
