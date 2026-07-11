// Port of Java data/scripts/system/handlers/quest/verteron/_1170HeadlessStoneStatue.java
// (Rolandas, reworked vlog). Talk to the Headless Stone Statue (730000) to start; use the Head
// of Stone Statue (700033) to get the fragment item (flips to REWARD); return to the statue,
// play movie 16, remove the item and finish on movie end.
using System.Linq;
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

namespace Quest.Verteron;

public sealed class _1170HeadlessStoneStatue : QuestHandlerBase
{
    private const int QuestIdConst = 1170;
    private const int StatueObj    = 730000;
    private const int HeadObj      = 700033;
    private const int FragmentItem = 182200504;

    private readonly IItemDao _itemDao;

    public _1170HeadlessStoneStatue(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StatueObj).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StatueObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HeadObj).OnTalk.Add(QuestId);
        engine.RegisterOnQuestMovieEnd(16, QuestId);
        engine.RegisterItemGet(FragmentItem, QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        var entry = player.Quests.Get(QuestId);
        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry is null)
        {
            if (targetId == StatueObj)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == HeadObj)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await GiveQuestItemAsync(player, conn, _itemDao, FragmentItem, 1, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StatueObj)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                await PlayQuestMovieAsync(conn, player, 16, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnItemGetAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != FragmentItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
        return true;
    }

    public override async ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        if (movieId != 16) return false;
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.REWARD) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, FragmentItem, 1, ct);
        return await FinishQuestAsync(conn, player, 0, ct);
    }
}
