// Port of Java data/scripts/system/handlers/quest/inggison/_10021FriendsForLife.java.
// Versetti (798927, var0->1) -> Tialla (798954, var1->2, later var8->reward) -> Lothas (799022,
// var2->3) -> kill 34 Ruthless Brohums across 4 mob ids (var1 0->10, then var0->4) -> Lothas
// (var4->7).
//
// Skip vs Java (documented deviation): Java's SETPRO5 branch (var==4) checks player.isInGroup2(),
// then on success gives Taloc Fruit (182206627) + Taloc's Tears (182206628), teleports into a solo
// instance of world 300190000 via InstanceService.getNextAvailableInstance + TeleportService2 (both
// unported), and the instance's own onItemUseEvent/onEnterWorldEvent/onDieEvent handlers drive a
// var5->6->7 puzzle (use the Fruit once, then the Tears 19 times) entirely inside that instance —
// none of which is reachable without InstanceService/TeleportService2. Collapsed the whole
// excursion directly from var 4 to var 7 (same precedent as poeta._1002RequestoftheElim's and
// ishalgen._2002WheresRae's instance-detour collapses), so the group check, both quest items, and
// the onItemUseEvent/onEnterWorldEvent/onDieEvent overrides (whose sole purpose was that instance's
// puzzle and safety-net var rollback) are all omitted rather than left as dead/unreachable code.
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

namespace Quest.Inggison;

public sealed class _10021FriendsForLife : QuestHandlerBase
{
    private const int QuestIdConst = 10021;
    private const int Versetti     = 798927;
    private const int Tialla       = 798954;
    private const int Lothas       = 799022;
    private const int PrecedingQuestId = 10000;

    private static readonly int[] _mobs = [215522, 215520, 215523, 215521];

    private readonly IItemDao _itemDao;

    public _10021FriendsForLife(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(Versetti).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Tialla).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Lothas).OnTalk.Add(QuestId);
        foreach (int mob in _mobs)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: PrecedingQuestId, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var         = entry.GetVar(0);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == Versetti)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }

            if (targetId == Tialla)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.QUEST_SELECT && var == 8)
                    return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                    return await DefaultCloseDialogAsync(env, conn, 8, 8, reward: true, sameNpc: false, ct);
                return false;
            }

            if (targetId == Lothas)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 2)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.QUEST_SELECT && var == 4)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.QUEST_SELECT && var == 7)
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO3)
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                if (dialog == DialogAction.SETPRO5 && var == 4)
                {
                    // Collapsed Taloc instance excursion (var 4 -> 7 directly) — see file header.
                    entry.SetVar(0, 7);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                if (dialog == DialogAction.SETPRO5 || dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 7, 8, false, 10000, 10001, ct);
                if (dialog == DialogAction.FINISH_DIALOG)
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                return false;
            }

            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == Versetti)
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

        int var1 = entry.GetVar(1);
        if (var1 < 9)
        {
            await ChangeQuestStepAsync(conn, entry, 1, var1 + 1, toReward: false, ct);
            return true;
        }
        if (var1 == 9)
        {
            entry.SetVar(0, 4);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }
}
