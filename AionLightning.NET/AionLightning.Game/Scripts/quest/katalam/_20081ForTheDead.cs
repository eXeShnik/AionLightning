// Port of Java data/scripts/system/handlers/quest/katalam/_20081ForTheDead.java (Cheatkiller).
// Asmodian mirror of _10081Aftermath: preceding quest 20080 unlocks this on level-up (20080
// itself is deferred — needs teleport infra not wired to quest scripts). Talk chain 800529 ->
// 800531 -> 801239 (801239 also turns in); killing any of 4 mobs bumps var from 4 up to 6,
// which flips straight to REWARD.
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

namespace Quest.Katalam;

public sealed class _20081ForTheDead : QuestHandlerBase
{
    private const int QuestIdConst = 20081;
    private const int FirstNpc     = 800529;
    private const int SecondNpc    = 800531;
    private const int TurnInNpc    = 801239;
    private const int SilentNpc    = 701535;
    private static readonly int[] MobIds = [232479, 232480, 232481, 233194];

    private readonly IItemDao _itemDao;

    public _20081ForTheDead(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npcId in new[] { FirstNpc, SecondNpc, TurnInNpc, SilentNpc })
            engine.RegisterQuestNpc(npcId).OnTalk.Add(QuestId);
        foreach (int mobId in MobIds)
            engine.RegisterQuestNpc(mobId).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 20080, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is not null && entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == FirstNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 2, false, 10000, 10001, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                return false;
            }
            if (targetId == SilentNpc) return true;
            if (targetId == SecondNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 2)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3)
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                return false;
            }
            if (targetId == TurnInNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 3)
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (dialog == DialogAction.SETPRO6)
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                return false;
            }
        }
        else if (entry is not null && entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
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

        int var = entry.GetVar(0);
        if (var < 4) return false;

        await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
        if (var == 6)
        {
            entry.SetVar(0, 6);
            entry.Status = QuestStatus.REWARD;
            await UpdateQuestStatusAsync(conn, entry, ct);
        }
        return true;
    }
}
