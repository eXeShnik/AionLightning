// Port of Java data/scripts/system/handlers/quest/rider_quests/_14014TurningTheIde.java (pralinka).
// Zone-mission sub-quest of 14010: Estino (203146) gives a Transformation Potion (var0 0->1); using
// it inside the Tursin Outpost entrance zone (var0 1->2, movie 18) transforms the player; report to
// Meteina (203147, var0 2->3); collect-check + Livanon (802045, var0 3->5); kill 2 Tursin soldiers
// (210178/216892, var0 5->7 then straight to REWARD); turn in at Morai (203164).
// Java bug: onDialogEvent's switch on Estino (203146) had no break after the QUEST_SELECT case, so
// talking with var0!=0 fell through into the SETPRO1 body and granted a duplicate Transformation
// Potion before the (failing) step-guard aborted. Fixed here so the item-give only fires on an
// actual SETPRO1 dialog.
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

namespace Quest.RiderQuests;

public sealed class _14014TurningTheIde : QuestHandlerBase
{
    private const int QuestIdConst = 14014;
    private const int EstinoNpc  = 203146;
    private const int MeteinaNpc = 203147;
    private const int LivanonNpc = 802045;
    private const int MoraiNpc   = 203164;
    private const int PotionItem = 182200505;
    private const string TursinOutpostEntranceZone = "TURSIN_OUTPOST_ENTRANCE_210030000";
    private static readonly int[] _mobs = [210178, 216892];

    private readonly IItemDao _itemDao;

    public _14014TurningTheIde(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npc in new[] { EstinoNpc, MeteinaNpc, LivanonNpc, MoraiNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(PotionItem, QuestId);
        foreach (int mob in _mobs) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 14010, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == EstinoNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 0 && await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    if (var != 0) return false;
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, PotionItem, 1, ct)) return false;
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                return false;
            }

            if (targetId == MeteinaNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return var == 2 && await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3)
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                return false;
            }

            if (targetId == LivanonNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 3 && await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 3, 5, false, 10000, 10001, ct);
                if (dialog == DialogAction.FINISH_DIALOG)
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == MoraiNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != PotionItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;
        if (!player.CurrentZones.Contains(TursinOutpostEntranceZone)) return false;

        var env = new QuestEnv(null, player, QuestId, 0);
        return await UseQuestObjectAsync(env, conn, 1, 2, reward: false, varNum: 0,
            addItemId: 0, addItemCount: 0, removeItemId: PotionItem, removeItemCount: 1,
            movieId: 18, dieObject: false, _itemDao, ct);
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var >= 5 && var < 7)
            return await DefaultOnKillEventAsync(env, conn, _mobs, 5, 7, ct);
        if (var == 7)
        {
            entry.Status = QuestStatus.REWARD;
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }
}
