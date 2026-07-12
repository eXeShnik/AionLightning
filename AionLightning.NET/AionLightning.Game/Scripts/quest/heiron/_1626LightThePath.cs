// Port of Java data/scripts/system/handlers/quest/heiron/_1626LightThePath.java.
// Accepting from 204592 hands over the torch item 182201788 if not already carried; use the seven
// waypoint objects (700221-700227) in sequence while holding it (var0->6, the last flips to
// REWARD); turn in at 204592.
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

namespace Quest.Heiron;

public sealed class _1626LightThePath : QuestHandlerBase
{
    private const int QuestIdConst = 1626;
    private const int StartNpc = 204592;
    private const int TorchItem = 182201788;
    private static readonly int[] Waypoints = { 700221, 700222, 700223, 700224, 700225, 700226, 700227 };

    private readonly IItemDao _itemDao;

    public _1626LightThePath(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        foreach (int npcId in Waypoints)
            engine.RegisterQuestNpc(npcId).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var entry  = player.Quests.Get(QuestId);
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                if (dialog == DialogAction.QUEST_ACCEPT_1 && player.Inventory.FindByItemId(TorchItem) is null or { Count: 0 })
                {
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, TorchItem, 1, ct)) return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            bool hasTorch = player.Inventory.FindByItemId(TorchItem) is { Count: 1 };
            if (dialog != DialogAction.USE_OBJECT || !hasTorch) return false;

            for (int i = 0; i < Waypoints.Length; i++)
            {
                if (targetId != Waypoints[i]) continue;
                bool isLast = i == Waypoints.Length - 1;
                return await UseQuestObjectAsync(env, conn, i, isLast ? i : i + 1, isLast, 0, ct);
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }
}
