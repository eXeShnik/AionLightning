// Port of Java data/scripts/system/handlers/quest/inggison/_11006TestingTheWaters.java.
// Talk to 798940 to start (accept gives 182206704 x1); using it swaps it for 182206706 (var0->1);
// talking again gives 182206705 and removes 182206706 (var1->2); using 182206705 removes it, gives
// 182206707 and flips to reward; turn-in at 798940 removes 182206707.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Inggison;

public sealed class _11006TestingTheWaters : QuestHandlerBase
{
    private const int QuestIdConst  = 11006;
    private const int StartNpc      = 798940;
    private const int SampleItem    = 182206704;
    private const int ContainerItem = 182206706;
    private const int PreparedItem  = 182206705;
    private const int ResultItem    = 182206707;

    private readonly IItemDao _itemDao;

    public _11006TestingTheWaters(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(SampleItem, QuestId);
        engine.RegisterQuestItem(PreparedItem, QuestId);
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId != StartNpc) return false;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                if (!await StartMissionAsync(conn, player, QuestStatus.START, ct)) return false;
                await GiveQuestItemAsync(player, conn, _itemDao, SampleItem, 1, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 1)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO2)
            {
                await GiveQuestItemAsync(player, conn, _itemDao, PreparedItem, 1, ct);
                await RemoveQuestItemAsync(player, conn, _itemDao, ContainerItem, 1, ct);
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, ResultItem, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (itemId == SampleItem && entry.GetVar(0) == 0)
        {
            await RemoveQuestItemAsync(player, conn, _itemDao, SampleItem, 1, ct);
            if (!await GiveQuestItemAsync(player, conn, _itemDao, ContainerItem, 1, ct)) return false;
            await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
            return true;
        }
        if (itemId == PreparedItem && entry.GetVar(0) == 2)
        {
            await RemoveQuestItemAsync(player, conn, _itemDao, PreparedItem, 1, ct);
            if (!await GiveQuestItemAsync(player, conn, _itemDao, ResultItem, 1, ct)) return false;
            await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
            return true;
        }
        return false;
    }
}
