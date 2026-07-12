// Port of Java data/scripts/system/handlers/quest/inggison/_10020ProvingYourselfToOutremus.java.
// Outremus (798926, var0->1) -> Yulia (798928, var1->2 gives item 182206301, later var4->5) ->
// repair the three Obelisks (730223/730224/730225, each marks its own var1/var2/var3 flag and the
// last one to flip sets var0=3) -> kill 14 Basrasa laborers (var1 0->10) and 2 Armored Spallers
// (var2 0->2), whichever finishes last sets var0=4 -> Yulia (var4->5, removes the item) -> Versetti
// (798927, var5->6, later var10->reward removing the item again) -> Marica (798955, var6->7) ->
// install the Obelisk at the three Fountainhead supports (700628/629/630, var7->8->9->10) -> back to
// Versetti -> report to Outremus for the reward.
// Java fallthrough idiom preserved as combined if-checks below (QUEST_SELECT falling into the
// SETPRO case's own step-gated helper when the var doesn't match the dialog page condition) —
// harmless since the helper itself re-validates the var and no-ops otherwise.
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

public sealed class _10020ProvingYourselfToOutremus : QuestHandlerBase
{
    private const int QuestIdConst = 10020;
    private const int Outremus     = 798926;
    private const int Yulia        = 798928;
    private const int StoppedObelisk     = 730223;
    private const int OverheatedObelisk  = 730224;
    private const int DeterioratedObelisk = 730225;
    private const int Versetti     = 798927;
    private const int Marica       = 798955;
    private const int EastSupport  = 700628;
    private const int WestSupport  = 700629;
    private const int NorthSupport = 700630;
    private const int RewardItem   = 182206301;
    private const int PrecedingQuestId = 10000;

    private static readonly int[] _laborerMobs =
        [215504, 216782, 215505, 216463, 216783, 216464, 216692, 215517, 216648, 215519, 216691, 215516, 216647, 215518];
    private static readonly int[] _spallerMobs = [215508, 215509];

    private readonly IItemDao _itemDao;

    public _10020ProvingYourselfToOutremus(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        int[] npcs = [Outremus, Yulia, StoppedObelisk, OverheatedObelisk, DeterioratedObelisk, Versetti, Marica, EastSupport, WestSupport, NorthSupport];
        foreach (int npc in npcs)
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        foreach (int mob in _laborerMobs)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
        foreach (int mob in _spallerMobs)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: PrecedingQuestId, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var         = entry.GetVar(0);
        int var1        = entry.GetVar(1);
        int var2        = entry.GetVar(2);
        int var3        = entry.GetVar(3);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == Outremus)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }

            if (targetId == Yulia)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.QUEST_SELECT && var == 4)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 1, 2, reward: false, sameNpc: false,
                        giveItemId: RewardItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                if (dialog == DialogAction.SETPRO5)
                    return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                return false;
            }

            if (targetId is StoppedObelisk or OverheatedObelisk or DeterioratedObelisk)
            {
                if (dialog != DialogAction.USE_OBJECT) return false;

                (int thisVarIdx, int otherVar1, int otherVar2) = targetId switch
                {
                    StoppedObelisk      => (1, var2, var3),
                    OverheatedObelisk   => (2, var1, var3),
                    _                   => (3, var1, var2),
                };
                int thisVar = entry.GetVar(thisVarIdx);
                if (var != 2 || thisVar != 0) return false;

                if (otherVar1 == 1 && otherVar2 == 1)
                    entry.SetVar(0, 3);
                else
                    entry.SetVar(thisVarIdx, 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }

            if (targetId == Versetti)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 5)
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (dialog == DialogAction.QUEST_SELECT && var == 10)
                    return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO6)
                    return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 10, 10, reward: true, sameNpc: false,
                        giveItemId: 0, giveItemCount: 0, removeItemId: RewardItem, removeItemCount: 1, ct);
                return false;
            }

            if (targetId == Marica)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 6)
                    return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO7)
                    return await DefaultCloseDialogAsync(env, conn, 6, 7, ct);
                return false;
            }

            if (targetId == EastSupport)
                return var == 7 && dialog == DialogAction.USE_OBJECT && await UseQuestObjectAsync(env, conn, 7, 8, false, 0, ct);
            if (targetId == WestSupport)
                return var == 8 && dialog == DialogAction.USE_OBJECT && await UseQuestObjectAsync(env, conn, 8, 9, false, 0, ct);
            if (targetId == NorthSupport)
                return var == 9 && dialog == DialogAction.USE_OBJECT && await UseQuestObjectAsync(env, conn, 9, 10, false, 0, ct);

            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == Outremus)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) != 3) return false;

        int targetId = env.TargetId;
        int var1 = entry.GetVar(1);
        int var2 = entry.GetVar(2);

        if (targetId == _spallerMobs[0] || targetId == _spallerMobs[1])
        {
            if (var2 < 2)
            {
                await ChangeQuestStepAsync(conn, entry, 2, var2 + 1, toReward: false, ct);
                return true;
            }
            if (var2 == 2 && var1 == 10)
            {
                entry.SetVar(0, 4);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
            // Java's redundant re-check (defaultOnKillEvent(spellers, 1, 2, 2)) here always fails
            // once var2==2 (its span no longer contains it) — ported as the same inert no-op.
            return false;
        }

        if (var1 < 9)
        {
            await ChangeQuestStepAsync(conn, entry, 1, var1 + 1, toReward: false, ct);
            return true;
        }
        if (var1 == 9)
        {
            if (var2 == 2)
            {
                entry.SetVar(0, 4);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
            await ChangeQuestStepAsync(conn, entry, 1, 10, toReward: false, ct);
            return true;
        }
        return false;
    }
}
