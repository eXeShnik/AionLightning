// Port of Java data/scripts/system/handlers/quest/rider_quests/_24013PoisonIntheWaters.java (pralinka).
// Zone-mission sub-quest of 24010: talk to Nokir (203631, var0 0->1, movie 63), talk to Shania
// (203621, gives a vial, var0 1->2), use the vial inside DF1A_ITEMUSEAREA_Q2016 (var0 2->3), kill 5
// mobs from the poison-well pack (var0 3->7 then straight to REWARD), turn in at Nokir.
// Java bug: onDialogEvent's switch on Nokir (203631) had no break after QUEST_SELECT, so a stray
// QUEST_SELECT sent while var0 != 0 fell through into SELECT_ACTION_1012's body and replayed movie
// 63 + reopened dialog 1012 unconditionally (that case has no var guard of its own). Likewise
// Shania's (203621) QUEST_SELECT fallthrough into SETPRO2 granted a duplicate vial before
// defaultCloseDialog's own var==1 guard could reject the transition. Fixed here so those side
// effects only fire on their own actual dialog id.
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

public sealed class _24013PoisonIntheWaters : QuestHandlerBase
{
    private const int QuestIdConst = 24013;
    private const int NokirNpc  = 203631;
    private const int ShaniaNpc = 203621;
    private const int VialItem  = 182215359;
    private const string PoisonWellZone = "DF1A_ITEMUSEAREA_Q2016";
    private static readonly int[] _mobs = [210455, 210456, 214039, 210458, 214032];

    private readonly IItemDao _itemDao;

    public _24013PoisonIntheWaters(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(NokirNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ShaniaNpc).OnTalk.Add(QuestId);
        foreach (int mob in _mobs) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
        engine.RegisterQuestItem(VialItem, QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 24010, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var = entry.GetVar(0);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == NokirNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SELECT_ACTION_1012)
                {
                    await PlayQuestMovieAsync(conn, player, 63, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1012, ct);
                }
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == ShaniaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    if (var != 1) return false;
                    await GiveQuestItemAsync(player, conn, _itemDao, VialItem, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                }
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == NokirNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var = entry.GetVar(0);
        if (var >= 3 && var < 7)
            return await DefaultOnKillEventAsync(env, conn, _mobs, 3, 7, ct);
        if (var == 7)
        {
            entry.Status = QuestStatus.REWARD;
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != VialItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 2) return false;
        if (!player.CurrentZones.Contains(PoisonWellZone)) return false;

        var env = new QuestEnv(null, player, QuestId, 0);
        return await UseQuestObjectAsync(env, conn, 2, 3, reward: false, varNum: 0,
            addItemId: 0, addItemCount: 0, removeItemId: itemId, removeItemCount: 1,
            movieId: 0, dieObject: false, _itemDao, ct);
    }
}
