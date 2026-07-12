// Port of Java data/scripts/system/handlers/quest/tiamaranta/_41558PunishOrPardon.java
// (Cheatkiller). Talk to 205914 to start; at 205894 receive the tool item 182212527 (var 0->1) or
// skip straight to var 2; kill 218725 to advance var 4 (if reached) to REWARD; picking up item
// 182212528 while at var 3 advances to var 4; using the tool item while at var 1 flips straight to
// REWARD; turn in at either 205894 (default reward) or 205914 (explicit reward index 1, removing
// item 182212528 first).
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

namespace Quest.Tiamaranta;

public sealed class _41558PunishOrPardon : QuestHandlerBase
{
    private const int QuestIdConst = 41558;
    private const int StartNpc     = 205914;
    private const int JudgeNpc     = 205894;
    private const int LootObj      = 701324;
    private const int KillNpc      = 218725;
    private const int ToolItemId   = 182212527;
    private const int GetItemId    = 182212528;

    private readonly IItemDao _itemDao;

    public _41558PunishOrPardon(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(ToolItemId, QuestId);
        engine.RegisterItemGet(GetItemId, QuestId);
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(JudgeNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(LootObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KillNpc).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == JudgeNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO10)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, ToolItemId, 1, ct);
                    await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                }
                if (dialog == DialogAction.SETPRO20)
                    return await DefaultCloseDialogAsync(env, conn, 0, 2, ct);
                return false;
            }

            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3)
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                return false;
            }

            if (targetId == LootObj) return true; // loot passthrough

            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == JudgeNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4081, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4166, ct);
                await RemoveQuestItemAsync(player, conn, _itemDao, GetItemId, 1, ct);
                return await FinishQuestAsync(conn, player, 1, ct);
            }
        }

        return false;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, KillNpc, 4, reward: true, ct);

    public override async ValueTask<bool> OnItemGetAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != GetItemId) return false;

        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 3) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != ToolItemId) return false;

        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, ToolItemId, 1, ct);
        await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: true, ct);
        return true;
    }
}
