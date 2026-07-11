// Port of Java data/scripts/system/handlers/quest/morheim/_2316VivisBook.java.
// Item-use start (182204115); talk to 204386 (SELECT_QUEST_REWARD removes the book, sets var 1,
// flips to REWARD); further dialog events at 204386 complete via the normal end-dialog flow.
// Skip vs Java: the 3s SM_ITEM_USAGE_ANIMATION + ThreadPoolManager-scheduled follow-up dialog is
// collapsed into an immediate response — no scheduled-task primitive exists for quest scripts in
// this port and the delay is purely cosmetic (the quest state change happens inside the callback
// either way).
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Morheim;

public sealed class _2316VivisBook : QuestHandlerBase
{
    private const int QuestIdConst = 2316;
    private const int TurnInNpc    = 204386;
    private const int BookItemId   = 182204115;

    private readonly IItemDao _itemDao;

    public _2316VivisBook(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(BookItemId, QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != BookItemId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is not null) return false;

        await conn.SendAsync(new SM_DIALOG_WINDOW(0, 4, QuestId), ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == 0)
        {
            if (env.DialogId != (int)DialogAction.QUEST_ACCEPT_1) return false;
            await StartMissionAsync(conn, player, QuestStatus.START, ct);
            await conn.SendAsync(new SM_DIALOG_WINDOW(0, 0), ct);
            return true;
        }

        if (targetId == TurnInNpc && entry is not null)
        {
            if (dialog == DialogAction.QUEST_SELECT && entry.Status == QuestStatus.START)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD && entry.Status != QuestStatus.COMPLETE)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, BookItemId, 1, ct);
                entry.SetVar(0, 1);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
