// Port of Java data/scripts/system/handlers/quest/heiron/_1052RootoftheRot.java. Mission-chain
// quest (no NPC quest-offer dialog): started only via zone-mission-end/level-up re-checks gated on
// quest 1500. Collect 3x182201603 + 3x182201604 for 204549 (var0->1->2), turn in at 730026
// (var2->REWARD); removing the collected items happens at the final turn-in (730024).
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Heiron;

public sealed class _1052RootoftheRot : QuestHandlerBase
{
    private const int QuestIdConst = 1052;
    private const int MainNpc = 204549;
    private const int MidNpc  = 730026;
    private const int EndNpc  = 730024;
    private const int CollectItemOne = 182201603;
    private const int CollectItemTwo = 182201604;

    private readonly IItemDao _itemDao;

    public _1052RootoftheRot(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(MainNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MidNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EndNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 1500, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);
        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == EndNpc)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, CollectItemOne, 3, ct);
                await RemoveQuestItemAsync(player, conn, _itemDao, CollectItemTwo, 3, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }
        if (entry.Status != QuestStatus.START) return false;

        if (targetId == MainNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                return false;
            }
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 1, false, 10000, 10001, ct);
            if (dialog == DialogAction.SETPRO1 && var == 0)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            if (dialog == DialogAction.SETPRO2 && var == 1)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
        }
        else if (targetId == MidNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 2)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SET_SUCCEED && var == 2)
            {
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
        }
        return false;
    }
}
