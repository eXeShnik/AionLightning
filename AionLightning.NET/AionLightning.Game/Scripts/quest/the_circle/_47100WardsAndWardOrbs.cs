// Port of Java data/scripts/system/handlers/quest/the_circle/_47100WardsAndWardOrbs.java (Cheatkiller).
// Board-accept daily (targetId 0, no start npc): kill 700970 for a 100%/5-cap side-quest drop of
// item 182211036, hand it in at 799881 (CHECK_USER_HAS_QUEST_ITEM -> checkQuestItems).
// Skip vs Java: the targetId==700970 mentor/mentee branch (player.isInGroup2() + isMentor() +
// GroupConfig.GROUP_MAX_DISTANCE) has no state-changing effect in Java either way (it either
// swallows the dialog with a bare `return true` or sends STR_MSG_DailyQuest_Ask_Mentee) - this
// port's Player/PlayerGroup model has no mentor/mentee concept at all (see
// MentorMonsterHuntHandler's documented limitation), so the branch is dropped entirely rather than
// invented; quest progress is unaffected since the drop itself (RegisterQuestDrop) is what advances
// this quest, not this dialog branch.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.TheCircle;

public sealed class _47100WardsAndWardOrbs : QuestHandlerBase
{
    private const int QuestIdConst = 47100;
    private const int DropNpc      = 700970;
    private const int TurnInNpc    = 799881;
    private const int DropItemId   = 182211036;

    private readonly IItemDao _itemDao;

    public _47100WardsAndWardOrbs(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        RegisterQuestDrop(engine, DropNpc, DropItemId, 5, 100);
        engine.RegisterQuestNpc(DropNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == 0 && env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(0, 0), ct);
                return true;
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId != TurnInNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 1, true, 5, 2716, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != TurnInNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
