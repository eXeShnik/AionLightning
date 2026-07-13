// Port of Java data/scripts/system/handlers/quest/beluslan/_2053AMissingFather.java.
// Mani (204707, var0->1, var5->6), Paeru (204749, var1->2, gives 182204305), reading that bottle
// (var2->3), entering Malek Mine zone (var3->4), Strahein's Liquor Bottle object 730108 (var4->5,
// swaps 182204305 for 182204306), back to Mani (var5->6, removes 182204306), Hammel (204800,
// var6->7); entering the Mine Port zone at var7 schedules movie 236 after 10s, whose end flips to
// REWARD and removes 182204307. Skip vs Java: the Secret Port Entrance (700359) teleport shortcut
// (usable at var7 if carrying >=1 182204307) is omitted — no TeleportService ported yet; players
// reach the mine port zone the normal way instead.
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

namespace Quest.Beluslan;

public sealed class _2053AMissingFather : QuestHandlerBase
{
    private const int QuestIdConst = 2053;
    private const int ManiNpc = 204707;
    private const int PaeruNpc = 204749;
    private const int BottleObj = 730108;
    private const int HammelNpc = 204800;
    private const int BottleItem = 182204305;
    private const int SwappedBottleItem = 182204306;
    private const int MineEntranceItem = 182204307;
    private const int MovieId = 236;
    private const string MalekMineZone = "MALEK_MINE_220040000";
    private const string MinePortZone = "MINE_PORT_220040000";

    private readonly IItemDao _itemDao;

    public _2053AMissingFather(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestItem(BottleItem, QuestId);
        engine.RegisterOnQuestMovieEnd(MovieId, QuestId);
        engine.RegisterOnQuestTimerEnd(QuestId);
        RegisterOnEnterZone(engine, MalekMineZone);
        RegisterOnEnterZone(engine, MinePortZone);
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterQuestNpc(ManiNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(PaeruNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HammelNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BottleObj).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var = entry.GetVar(0);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == ManiNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 5) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                if (dialog == DialogAction.SETPRO6)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 5, 6, reward: false, sameNpc: false,
                        giveItemId: 0, giveItemCount: 0, removeItemId: SwappedBottleItem, removeItemCount: 1, ct);
                return false;
            }
            if (targetId == PaeruNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 1, 2, reward: false, sameNpc: false,
                        giveItemId: BottleItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                return false;
            }
            if (targetId == BottleObj)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO5)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 4, 5, reward: false, sameNpc: false,
                        giveItemId: SwappedBottleItem, giveItemCount: 1, removeItemId: BottleItem, removeItemCount: 1, ct);
                return false;
            }
            if (targetId == HammelNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 6) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                if (dialog == DialogAction.SETPRO7) return await DefaultCloseDialogAsync(env, conn, 6, 7, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == ManiNpc)
            {
                if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != BottleItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 2) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, BottleItem, 1, ct);
        entry.SetVar(0, 3);
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (zoneName == MalekMineZone)
        {
            if (var != 3) return false;
            await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: false, ct);
            return true;
        }
        if (zoneName == MinePortZone)
        {
            if (var != 7) return false;
            StartQuestTimer(env, conn, 10);
            return false;
        }
        return false;
    }

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        await PlayQuestMovieAsync(conn, env.Player, MovieId, ct);
        return true;
    }

    public override async ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        if (movieId != MovieId) return false;
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        await RemoveQuestItemAsync(env.Player, conn, _itemDao, MineEntranceItem, 1, ct);
        entry.SetVar(0, 7);
        entry.Status = QuestStatus.REWARD;
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 2500, isZoneMission: true, ct);
}
