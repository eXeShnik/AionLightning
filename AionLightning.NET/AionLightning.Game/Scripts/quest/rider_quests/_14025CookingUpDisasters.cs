// Port of Java data/scripts/system/handlers/quest/rider_quests/_14025CookingUpDisasters.java (pralinka).
// Zone-mission sub-quest of 14020 (gated on 14024 also being complete): talk to Tumblusen (203989,
// var0 0->1), collect-check (var0 1->3), report to Mabangtah (204020, var0 3->4, hands out an
// item), kill 4 Kaidan (var index1) and 1 Kalabar (var index2) while var0==5, return to Tumblusen
// (movie 36, item removed, var0 4->5), turn in at Telemachus (203901).
// Java bug: onDialogEvent's switch on Tumblusen (203989) had no breaks, so a QUEST_SELECT/
// CHECK_USER_HAS_QUEST_ITEM/SETPRO1 dialog sent while var0 was outside their handled values fell
// through into the SETPRO4 body and played movie 36 + removed item 182201005 unconditionally
// before defaultCloseDialog's var==4 guard failed. Likewise Mabangtah's (204020) QUEST_SELECT
// fallthrough into SETPRO3 granted item 182201005 unconditionally. Fixed here so those side
// effects only fire on their own actual dialog id.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.RiderQuests;

public sealed class _14025CookingUpDisasters : QuestHandlerBase
{
    private const int QuestIdConst = 14025;
    private const int TumblusenNpc  = 203989;
    private const int MabangtahNpc  = 204020;
    private const int TelemachusNpc = 203901;
    private const int EvidenceItem  = 182201005;
    private static readonly int[] _kaidan  = [212025, 212029, 212039];
    private const int KalabarNpc = 212351;

    private readonly IItemDao _itemDao;

    public _14025CookingUpDisasters(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npc in new[] { TumblusenNpc, MabangtahNpc, TelemachusNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        foreach (int mob in _kaidan) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(KalabarNpc).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, precedingQuestId: 14024, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, (System.Collections.Generic.IReadOnlyCollection<int>)[14020, 14024], isZoneMission: true, ct);

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 5) return false;

        int targetId = env.TargetId;
        if (targetId == 212025 || targetId == 212029 || targetId == 212039)
            return await BumpVarAsync(conn, entry, 1, 0, 4, ct);
        if (targetId == KalabarNpc)
            return await BumpVarAsync(conn, entry, 2, 0, 1, ct);
        return false;
    }

    private async ValueTask<bool> BumpVarAsync(GsClientConnection conn, QuestEntry entry, int varNum, int startVar, int endVar, CancellationToken ct)
    {
        int var = entry.GetVar(varNum);
        if (var < startVar || var >= endVar) return false;
        await ChangeQuestStepAsync(conn, entry, varNum, var + 1, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        if (entry.Status == QuestStatus.REWARD)
            return env.TargetId == TelemachusNpc && await SendQuestEndDialogAsync(env, conn, ct);
        if (entry.Status != QuestStatus.START) return false;

        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        int var0 = entry.GetVar(0), var1 = entry.GetVar(1), var2 = entry.GetVar(2);

        if (env.TargetId == TumblusenNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var0 == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (var0 == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (var0 == 4) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (var0 == 5 && var1 == 4 && var2 == 1) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                return false;
            }
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return var0 == 1 && await CheckQuestItemsAsync(env, conn, _itemDao, 1, 3, reward: false, checkOkId: 10, checkFailId: 0, ct);
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            if (dialog == DialogAction.SETPRO4)
            {
                if (var0 != 4) return false;
                await PlayQuestMovieAsync(conn, player, 36, ct);
                await RemoveQuestItemAsync(player, conn, _itemDao, EvidenceItem, 1, ct);
                return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
            }
            if (dialog == DialogAction.SETPRO6)
                return await DefaultCloseDialogAsync(env, conn, 5, 5, reward: true, sameNpc: false, ct);
            if (dialog == DialogAction.FINISH_DIALOG)
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            return false;
        }

        if (env.TargetId == MabangtahNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return var0 == 3 && await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SETPRO3)
            {
                if (var0 != 3) return false;
                if (!await GiveQuestItemAsync(player, conn, _itemDao, EvidenceItem, 1, ct)) return false;
                return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
            }
            return false;
        }

        return false;
    }
}
