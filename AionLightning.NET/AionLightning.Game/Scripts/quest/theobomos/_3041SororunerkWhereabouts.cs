// Port of Java data/scripts/system/handlers/quest/theobomos/_3041SororunerkWhereabouts.java.
// Talk to Sororunerk (798167) to start; using the object (700378) gives the Torn Note
// (182208031, if not already held) and flips straight to REWARD; return to Sororunerk to finish.
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

namespace Quest.Theobomos;

public sealed class _3041SororunerkWhereabouts : QuestHandlerBase
{
    private const int QuestIdConst = 3041;
    private const int SororunerkNpc = 798167;
    private const int NoteObject    = 700378;
    private const int NoteItemId    = 182208031;

    private readonly IItemDao _itemDao;

    public _3041SororunerkWhereabouts(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(SororunerkNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(SororunerkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(NoteObject).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != SororunerkNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == NoteObject)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return entry.GetVar(0) == 0 && await SendQuestDialogAsync(conn, targetObjId, 1352, ct);

            if (dialog == DialogAction.SETPRO1)
            {
                long count = player.Inventory.FindByItemId(NoteItemId)?.Count ?? 0;
                if (count == 0 && !await GiveQuestItemAsync(player, conn, _itemDao, NoteItemId, 1, ct))
                    return true;

                entry.SetVar(0, entry.GetVar(0) + 1);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == SororunerkNpc)
        {
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
