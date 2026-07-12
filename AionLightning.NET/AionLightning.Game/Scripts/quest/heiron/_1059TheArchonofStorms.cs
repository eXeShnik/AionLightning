// Port of Java data/scripts/system/handlers/quest/heiron/_1059TheArchonofStorms.java.
// Talk to 204505 (var0->1), 204533 (var1->2), use object 700282 to play movie 193 (which sets
// var3), 204533 again (var3->4), then 204535 gives item 182201619 (var4->5); using that item
// finishes the quest (removes it, flips to REWARD). Mission-chain quest (no NPC quest-offer
// dialog), gated on 1500. Skip vs Java: the item-use zone check (LF3_ITEMUSEAREA_Q1059) and the
// 15s transformation buff applied on movie end aren't ported (no zone-shape or direct skill-apply
// infra yet) — the completion logic itself is unaffected.
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

namespace Quest.Heiron;

public sealed class _1059TheArchonofStorms : QuestHandlerBase
{
    private const int QuestIdConst = 1059;
    private const int FirstNpc  = 204505;
    private const int SecondNpc = 204533;
    private const int StormObjectNpc = 700282;
    private const int ThirdNpc  = 204535;
    private const int MovieId   = 193;
    private const int RelicItem = 182201619;

    private readonly IItemDao _itemDao;

    public _1059TheArchonofStorms(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnQuestMovieEnd(MovieId, QuestId);
        engine.RegisterQuestItem(RelicItem, QuestId);
        engine.RegisterQuestNpc(FirstNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SecondNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StormObjectNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ThirdNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 1500, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == FirstNpc) return await SendQuestEndDialogAsync(env, conn, ct);
            return false;
        }
        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (targetId == FirstNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1 && var == 0)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
        }
        else if (targetId == SecondNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO2 && var == 1)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            if (dialog == DialogAction.SETPRO4 && var == 3)
                return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
        }
        else if (targetId == StormObjectNpc && var == 2)
        {
            if (dialog == DialogAction.USE_OBJECT)
            {
                await PlayQuestMovieAsync(conn, player, MovieId, ct);
                return true;
            }
        }
        else if (targetId == ThirdNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 4)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SETPRO5 && var == 4)
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 4, 5, reward: false, sameNpc: false,
                    giveItemId: RelicItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        if (movieId != MovieId) return false;
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 2) return false;

        entry.SetVar(0, 3);
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != RelicItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status == QuestStatus.COMPLETE) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, RelicItem, 1, ct);
        entry.SetVar(0, 5);
        entry.Status = QuestStatus.REWARD;
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }
}
