// Port of Java data/scripts/system/handlers/quest/sarpan/_41253Remembrance.java (Cheatkiller).
// Talk to 205787 to start (no item); at 205572 hand in the quest_data.xml collect_items for item
// 182213107 (var 0->1->2); use object 730487 (var==2, SET_SUCCEED removes the item, spawns npc
// 701417 at the player's position, flips to REWARD); turn in at 205787.
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

namespace Quest.Sarpan;

public sealed class _41253Remembrance : QuestHandlerBase
{
    private const int QuestIdConst = 41253;
    private const int StartNpc      = 205787;
    private const int CollectorNpc  = 205572;
    private const int ShrineNpc     = 730487;
    private const int ItemId        = 182213107;
    private const int SpawnNpcId    = 701417;

    private readonly IItemDao _itemDao;

    public _41253Remembrance(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(CollectorNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ShrineNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == CollectorNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    int var = entry.GetVar(0);
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    return false;
                }
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM_SIMPLE)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 2, false, 10000, 10001, ItemId, 1, ct);
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == ShrineNpc)
            {
                if (dialog == DialogAction.USE_OBJECT && entry.GetVar(0) == 2)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SET_SUCCEED && entry.GetVar(0) == 2)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
                    var pos = player.Position;
                    SpawnQuestNpc(pos.WorldId, pos.InstanceId, SpawnNpcId, pos.X, pos.Y, pos.Z, (byte)pos.Heading);
                    return await DefaultCloseDialogAsync(env, conn, 2, 2, reward: true, sameNpc: false, ct);
                }
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
