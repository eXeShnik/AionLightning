// Port of Java data/scripts/system/handlers/quest/marchutan_priory/_47000AltgardOrbIt.java
// (Cheatkiller). Sidequest item drop from 700970 (182211033 x5 @100%); collect 1 at 799864 via
// CHECK_USER_HAS_QUEST_ITEM to flip to REWARD, then turn in.
// Skip vs Java: talking to 700970 while in a mentor group (isInGroup2/isMentor + GROUP_MAX_DISTANCE)
// silently "handles" the dialog when a mentor is nearby, or nags for one otherwise — this port's
// Player/PlayerGroup model has no mentor/mentee concept at all (same pre-existing gap documented in
// QuestEngine.Handlers.Templates.MentorMonsterHuntHandler), so that branch is omitted entirely; a
// player who is never considered "grouped with a mentor" here gets the same net result Java gives a
// non-grouped player (the block never returns, control falls through to the bottom `false`).
// qs.canRepeat() (daily-reset re-entry) isn't ported, matching every other repeatable quest already
// in this codebase.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.MarchutanPriory;

public sealed class _47000AltgardOrbIt : QuestHandlerBase
{
    private const int QuestIdConst  = 47000;
    private const int MentorNpc     = 700970; // group2/mentor-gated NPC — no reachable dialog case (see file header)
    private const int TurnInNpc     = 799864;
    private const int DropItemId    = 182211033;

    private readonly IItemDao _itemDao;

    public _47000AltgardOrbIt(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        RegisterQuestDrop(engine, MentorNpc, DropItemId, 5, 100, step: 0);
        engine.RegisterQuestNpc(MentorNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId == 0 && dialog == DialogAction.QUEST_ACCEPT_1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await CloseDialogWindowAsync(conn, 0, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == TurnInNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 1, reward: true, checkOkId: 5, checkFailId: 2716, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
