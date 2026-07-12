// Port of Java data/scripts/system/handlers/quest/tiamaranta/_41502HiddenConnections.java (mr.maddison).
// Talk to 205935 to accept (grants item 182212514 first, matching Java's give-then-start order);
// using the item while inside SMALL_CRATER advances var 0->1; interacting with 701129 at var 1
// flips straight to REWARD; turn in at 205935.
// Skip vs Java: both branches teleport the player (to 604/1018/210 on item use, to 329/541/362 at
// 701129) - no TeleportService2 equivalent is ported (same precedent as _1430ATeleportationExperiment
// in eltnen); the var/status transitions are kept so the quest stays completable without the
// teleport.
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

public sealed class _41502HiddenConnections : QuestHandlerBase
{
    private const int QuestIdConst = 41502;
    private const int StartNpc     = 205935;
    private const int RelayNpc     = 701129;
    private const int DeviceItemId = 182212514;
    private const string SmallCraterZone = "SMALL_CRATER_600030000";

    private readonly IItemDao _itemDao;

    public _41502HiddenConnections(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RelayNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(DeviceItemId, QuestId);
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
            if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
            {
                if (!await GiveQuestItemAsync(player, conn, _itemDao, DeviceItemId, 1, ct)) return true;
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == RelayNpc && entry.GetVar(0) == 1)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: true, ct);
            return true;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 0) return false;
        if (itemId != DeviceItemId || !player.CurrentZones.Contains(SmallCraterZone)) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
        return true;
    }
}
