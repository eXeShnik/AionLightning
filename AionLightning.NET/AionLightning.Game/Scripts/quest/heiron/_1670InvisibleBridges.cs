// Port of Java data/scripts/system/handlers/quest/heiron/_1670InvisibleBridges.java.
// Started by using quest item 182201770 (opens dialog 4, accept flips to START); then move within
// onAtDistance range of the three bridge markers 212281 -> 212282 -> 212283 in order
// (var0 0 -> 16 -> 48). Report to 203837: SELECT_QUEST_REWARD flips to REWARD (same npc), then
// turn in (removes the quest item).
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

namespace Quest.Heiron;

public sealed class _1670InvisibleBridges : QuestHandlerBase
{
    private const int QuestIdConst = 1670;
    private const int ReportNpc = 203837;
    private const int Marker1   = 212281;
    private const int Marker2   = 212282;
    private const int Marker3   = 212283;
    private const int QuestItem = 182201770;

    private readonly IItemDao _itemDao;

    public _1670InvisibleBridges(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(QuestItem, QuestId);
        engine.RegisterQuestNpc(ReportNpc).OnTalk.Add(QuestId);
        RegisterOnAtDistance(engine, Marker1);
        RegisterOnAtDistance(engine, Marker2);
        RegisterOnAtDistance(engine, Marker3);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (env.TargetId == 0 && dialog == DialogAction.QUEST_ACCEPT_1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (env.TargetId == ReportNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, reward: true, sameNpc: true, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (env.TargetId == ReportNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                await RemoveQuestItemAsync(player, conn, _itemDao, QuestItem, 1, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnAtDistanceAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (env.TargetId == Marker1 && var == 0)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 16, toReward: false, ct);
            return true;
        }
        if (env.TargetId == Marker2 && var == 16)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 48, toReward: false, ct);
            return true;
        }
        if (env.TargetId == Marker3 && var == 48)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 48, toReward: true, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != QuestItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status == QuestStatus.NONE)
            return await SendQuestDialogAsync(conn, 0, 4, ct);
        return false;
    }
}
