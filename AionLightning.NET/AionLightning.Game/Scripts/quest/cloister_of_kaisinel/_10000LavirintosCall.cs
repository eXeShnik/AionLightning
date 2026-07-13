// Port of Java data/scripts/system/handlers/quest/cloister_of_kaisinel/_10000LavirintosCall.java
// (dta3000, mod kale/vlog). Elyos Inggison campaign opener: auto-starts on entering world 110010000;
// talk to Eremita (798600) to accept, then to Lavirintos (203701) whose SET_SUCCEED gives item
// 182206300 and flips START->REWARD; turn in at Eremita (798600), which pokes the dependent Inggison
// zone missions (10001/10026/10020-10025) to start (Java onEnterZoneMissionEnd).
// Skip vs Java: sendQuestEndDialog(env, {182206300}) removes the work item on turn-in — the base
// end-dialog helper has no item-removal variant, so that cosmetic cleanup is dropped; the state
// transition (grant reward + COMPLETE) is intact.
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

namespace Quest.CloisterOfKaisinel;

public sealed class _10000LavirintosCall : QuestHandlerBase
{
    private const int QuestIdConst  = 10000;
    private const int LavirintosNpc = 203701;
    private const int EremitaNpc    = 798600;
    private const int StartWorldId  = 110010000;
    private const int WorkItem      = 182206300;

    private static readonly int[] DependentQuests = { 10001, 10026, 10020, 10021, 10022, 10023, 10024, 10025 };

    private readonly IItemDao _itemDao;
    private QuestEngine _engine = null!;

    public _10000LavirintosCall(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        _engine = engine;
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterQuestNpc(LavirintosNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(LavirintosNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EremitaNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        if (player.Position.WorldId != StartWorldId) return false;
        if (player.Quests.Get(QuestId) is not null) return false;
        return await StartMissionAsync(conn, player, QuestStatus.START, ct);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (targetId == EremitaNpc && entry is null)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry is null) return false;

        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == LavirintosNpc && var == 0)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                {
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, WorkItem, 1, ct))
                        return true;
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != EremitaNpc) return false;

            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);

            foreach (int quest in DependentQuests)
                await _engine.OnZoneMissionEndAsync(new QuestEnv(env.Target, player, quest, env.DialogId), conn, ct);

            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
