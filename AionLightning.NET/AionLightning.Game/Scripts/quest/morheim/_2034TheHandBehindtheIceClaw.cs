// Port of Java data/scripts/system/handlers/quest/morheim/_2034TheHandBehindtheIceClaw.java
// (Rhys2002, reworked vlog). Zone-mission chain quest (auto-started via
// OnLevelUpAsync/OnZoneMissionEndAsync once 2300 is COMPLETE). Talk Nina (204303, var 0->1), then
// Jorund (204332) hands out item 182204008 at var 1->2 (re-issued if still at var 2); using the
// "Dead Fire" object (700246) at var 2 with the Frozen Fossil (182204019) in hand spawns 204417 and
// consumes both items; killing 204417 advances var 2->3, back to Jorund for var 3->4 (grants title
// 58); killing 212877 advances var 4->5; back to Nina for the reward transition (var 5->5). Using
// item 182204008 while inside ALTAR_OF_TRIAL_220020000 also advances var 2->2 (a Java no-op
// transition that only consumes the item - preserved as-is). Turn in at Aegir (204301).
// Java quirk ported as-is: the 700246 USE_OBJECT case never returns from onDialogEvent in the
// original (falls out to the method's final `return false`), so no dialog ack is sent here either.
// Skip vs Java: player.getTitleList().addTitle(58, true, 0) is a persistent DB-backed title grant;
// the frozen quest-handler ctor has no ITitleDao, so this port only adds it to the in-memory
// OwnedTitles set (won't survive relogin without a separate title-sync pass).
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

namespace Quest.Morheim;

public sealed class _2034TheHandBehindtheIceClaw : QuestHandlerBase
{
    private const int QuestIdConst = 2034;
    private const int NinaNpc      = 204303;
    private const int JorundNpc    = 204332;
    private const int DeadFireNpc  = 700246;
    private const int AegirNpc     = 204301;
    private const int FrostbornNpc = 204417;
    private const int ExecutionerNpc = 212877;
    private const int GaleItem     = 182204008;
    private const int FossilItem   = 182204019;
    private const string TrialZone = "ALTAR_OF_TRIAL_220020000";

    private readonly IItemDao _itemDao;

    public _2034TheHandBehindtheIceClaw(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(FrostbornNpc).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(ExecutionerNpc).OnKill.Add(QuestId);
        engine.RegisterQuestItem(GaleItem, QuestId);
        foreach (int npc in new[] { NinaNpc, JorundNpc, DeadFireNpc, AegirNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 2300, isZoneMission: true, ct);

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        if (await DefaultOnKillEventAsync(env, conn, FrostbornNpc, 2, 3, ct)) return true;
        return await DefaultOnKillEventAsync(env, conn, ExecutionerNpc, 4, 5, ct);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != GaleItem) return false;
        if (!player.CurrentZones.Contains(TrialZone)) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null) return false;

        var env = new QuestEnv(null, player, QuestId, 0);
        return await UseQuestObjectAsync(env, conn, step: 2, nextStep: 2, reward: false, varNum: 0,
            addItemId: 0, addItemCount: 0, removeItemId: GaleItem, removeItemCount: 1, movieId: 0,
            dieObject: false, _itemDao, ct);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);
        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == NinaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 5) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                    return await DefaultCloseDialogAsync(env, conn, 5, 5, reward: true, sameNpc: false, ct);
                return false;
            }

            if (targetId == JorundNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 2)
                        return await SendQuestDialogAsync(conn, targetObjId,
                            (player.Inventory.FindByItemId(GaleItem)?.Count ?? 0) == 0 ? 1694 : 1693, ct);
                    if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO2)
                {
                    if (var == 1) return await DefaultCloseDialogAsync(env, conn, _itemDao, 1, 2, reward: false, sameNpc: false, GaleItem, 1, 0, 0, ct);
                    if (var == 2) return await DefaultCloseDialogAsync(env, conn, _itemDao, 2, 2, reward: false, sameNpc: false, GaleItem, 1, 0, 0, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO4)
                {
                    if (var != 3) return false;
                    player.OwnedTitles.Add(58);
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                }
                return false;
            }

            if (targetId == DeadFireNpc)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 2 &&
                    (player.Inventory.FindByItemId(FossilItem)?.Count ?? 0) > 0)
                {
                    var pos = env.Target!.Position;
                    SpawnQuestNpc(220020000, 1, FrostbornNpc, pos.X, pos.Y, pos.Z, (byte)pos.Heading);
                    await RemoveQuestItemAsync(player, conn, _itemDao, GaleItem, 1, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, FossilItem, 1, ct);
                }
                return false; // Java: no dialog ack sent from this branch either - see header note.
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == AegirNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
