// Port of Java data/scripts/system/handlers/quest/sarpan/_41177MapQuest.java (Cheatkiller).
// Started by using a map fragment item (182213104, no NPC involved) - accepting hands in the
// fragment at Devena (800127, var 0->1), giving a finished map (182213149) and a marker item
// (182213186); using the finished map spawns a delivery point (730473) at the player's position and
// flips to REWARD; turn it in there.
// Skip vs Java: the turn-in branch calls npc.getController().onDelete() on the spawned delivery
// point - no NPC despawn API is exposed to hand-written quest scripts in this port, so it's left
// standing (harmless leftover, not required for completability).
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

namespace Quest.Sarpan;

public sealed class _41177MapQuest : QuestHandlerBase
{
    private const int QuestIdConst  = 41177;
    private const int FragmentItemId = 182213104;
    private const int MapItemId      = 182213149;
    private const int MarkerItemId   = 182213186;
    private const int DevenaNpc      = 800127;
    private const int DeliveryPointNpc = 730473;

    private readonly IItemDao _itemDao;

    public _41177MapQuest(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(MapItemId, QuestId);
        engine.RegisterQuestItem(FragmentItemId, QuestId);
        engine.RegisterQuestNpc(DevenaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(DeliveryPointNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == 0 && dialog == DialogAction.QUEST_ACCEPT_1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await CloseDialogWindowAsync(conn, 0, ct);
            }
            return false;
        }

        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START && targetId == DevenaNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, FragmentItemId, 1, ct);
                await GiveQuestItemAsync(player, conn, _itemDao, MapItemId, 1, ct);
                await GiveQuestItemAsync(player, conn, _itemDao, MarkerItemId, 1, ct);
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == DeliveryPointNpc)
        {
            if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);

            await RemoveQuestItemAsync(player, conn, _itemDao, MapItemId, 1, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, MarkerItemId, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);

        if (itemId == FragmentItemId)
        {
            if (entry is null || entry.Status == QuestStatus.NONE)
                return await SendQuestDialogAsync(conn, 0, 4, ct);
            return false;
        }

        if (itemId == MapItemId)
        {
            if (entry is not null && entry.Status == QuestStatus.START)
            {
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                var pos = player.Position;
                SpawnQuestNpc(pos.WorldId, pos.InstanceId, DeliveryPointNpc, pos.X, pos.Y, pos.Z, (byte)pos.Heading);
                return true;
            }
            return false;
        }

        return false;
    }
}
