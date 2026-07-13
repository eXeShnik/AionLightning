// Port of Java data/scripts/system/handlers/quest/rider_quests/_24022SneakBehindTheIceClaw.java (pralinka).
// Zone-mission sub-quest of 24020: talk to Nina (203303... actually 204303, var0 0->1), talk to Orhe
// (204399, var0 1->2), talk to Jorund (204332, gives Hard Flint, var0 2->3), use a Dead Fire
// (700246, requires a prior item 182215365, spawns a Frost Wraith 204417, var0 3->4), kill 204417
// (var0 4->5), kill Landver (802047, gives title 58, var0 5->6), kill 212877 (var0 6->7), turn in at
// Nina (SET_SUCCEED, reward); separately hand back the Hard Flint at Aegir (204301) before the final
// turn-in dialog.
// Skip vs Java: player.getTitleList().addTitle(58, true, 0) - no player title-grant system in this
// port (PlayerTitlesData only holds static title templates); omitted as a non-gating cosmetic
// reward, same precedent as the disguise-buff omissions elsewhere in this batch.
// Java bug: the outer switch(targetId) case for the Dead Fire object (700246) had no break before
// "case 802047:", so any interaction with it that didn't match (USE_OBJECT, var==3, item in hand)
// fell through into Landver's dialog switch (running its own dialog logic against the wrong
// target). Fixed here by using independent per-NPC if-blocks that never fall into each other.
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

public sealed class _24022SneakBehindTheIceClaw : QuestHandlerBase
{
    private const int QuestIdConst = 24022;
    private const int NinaNpc     = 204303;
    private const int OrheNpc     = 204399;
    private const int JorundNpc   = 204332;
    private const int DeadFireNpc = 700246;
    private const int LandverNpc  = 802047;
    private const int AegirNpc    = 204301;
    private const int FrostWraithMob = 204417;
    private const int OtherMob       = 212877;
    private const int PriorItem = 182215365;
    private const int HardFlintItem = 182215364;
    private const string TrialZone = "ALTAR_OF_TRIAL_220020000";

    private readonly IItemDao _itemDao;

    public _24022SneakBehindTheIceClaw(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(FrostWraithMob).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(OtherMob).OnKill.Add(QuestId);
        engine.RegisterQuestItem(HardFlintItem, QuestId);
        foreach (int npc in new[] { NinaNpc, OrheNpc, JorundNpc, DeadFireNpc, AegirNpc, LandverNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 24020, isZoneMission: true, ct);

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
            if (targetId == NinaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 7) return await SendQuestDialogAsync(conn, targetObjId, 3399, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                    return await DefaultCloseDialogAsync(env, conn, 7, 7, reward: true, sameNpc: false, ct);
                return false;
            }
            if (targetId == OrheNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == JorundNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 2)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3 && var == 2)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 2, 3, reward: false, sameNpc: false,
                        giveItemId: HardFlintItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                return false;
            }
            if (targetId == DeadFireNpc)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 3 && player.Inventory.FindByItemId(PriorItem) is not null)
                {
                    var target = env.Target;
                    if (target is not null)
                        SpawnQuestNpc(220020000, 1, FrostWraithMob, target.Position.X, target.Position.Y, target.Position.Z, (byte)target.Position.Heading);
                    await RemoveQuestItemAsync(player, conn, _itemDao, PriorItem, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                }
                return false;
            }
            if (targetId == LandverNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 5)
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (dialog == DialogAction.SETPRO6)
                {
                    // Skip vs Java: player.getTitleList().addTitle(58, true, 0) - no title-grant system.
                    return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
                }
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == AegirNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, HardFlintItem, 1, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != HardFlintItem) return false;
        if (!player.CurrentZones.Contains(TrialZone)) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        var env = new QuestEnv(null, player, QuestId, 0);
        return await UseQuestObjectAsync(env, conn, 3, 4, reward: false, varNum: 0,
            addItemId: 0, addItemCount: 0, removeItemId: itemId, removeItemCount: 1,
            movieId: 0, dieObject: false, _itemDao, ct);
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int targetId = env.TargetId;
        if (targetId == FrostWraithMob)
            return await DefaultOnKillEventAsync(env, conn, FrostWraithMob, 4, 5, ct);
        if (targetId == OtherMob)
            return await DefaultOnKillEventAsync(env, conn, OtherMob, 6, 7, ct);
        return false;
    }
}
