// Port of Java data/scripts/system/handlers/quest/tiamaranta/_41530OneFroggyEvening.java (Cheatkiller).
// Talk to 205911 to accept (grants quest item 182212531); using that item while inside
// LDF4B_ITEMUSEAREA_Q41530A consumes it, advances var 0->1 and spawns 218421 beside the player; killing
// 218421 flips to REWARD; turn in at 205911.
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

public sealed class _41530OneFroggyEvening : QuestHandlerBase
{
    private const int QuestIdConst = 41530;
    private const int StartNpc     = 205911;
    private const int FrogNpc      = 218421;
    private const int ItemId       = 182212531;
    private const string ItemUseZone = "LDF4B_ITEMUSEAREA_Q41530A";

    private readonly IItemDao _itemDao;

    public _41530OneFroggyEvening(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(ItemId, QuestId);
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FrogNpc).OnKill.Add(QuestId);
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
                await GiveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
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
        return await DefaultOnKillEventAsync(env, conn, FrogNpc, 1, reward: true, ct);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (itemId != ItemId || !player.CurrentZones.Contains(ItemUseZone)) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
        await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
        var pos = player.Position;
        SpawnQuestNpc(pos.WorldId, pos.InstanceId, FrogNpc, pos.X + 2, pos.Y - 2, pos.Z, 0);
        return true;
    }
}
