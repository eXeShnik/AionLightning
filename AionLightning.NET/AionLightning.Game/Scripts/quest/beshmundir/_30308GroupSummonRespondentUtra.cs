// Port of Java data/scripts/system/handlers/quest/beshmundir/_30308GroupSummonRespondentUtra.java (Gigi).
// Asmodian counterpart of _30208GroupTheTruthHurts, identical shape at a different start npc: talk
// to 799322 to start (QUEST_SELECT first hands over the artifact item 182209710); using that item
// while in the Beshmundir Temple instance (world 300170000 - Java TODO: use zone instead of map)
// consumes it after a 3s delay and spawns 799506 at the player's position; talking to 799506
// (SET_SUCCEED) flips to REWARD; turn in at 799322.
// Skip vs Java: same despawn-on-talk and SM_ITEM_USAGE_ANIMATION omissions as
// _30208GroupTheTruthHurts - see that file's header for the established precedent.
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

namespace Quest.Beshmundir;

public sealed class _30308GroupSummonRespondentUtra : QuestHandlerBase
{
    private const int QuestIdConst  = 30308;
    private const int StartNpc      = 799322;
    private const int SpawnedNpc    = 799506;
    private const int ArtifactItem  = 182209710;
    private const int TempleWorldId = 300170000;

    private readonly IItemDao _itemDao;

    public _30308GroupSummonRespondentUtra(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SpawnedNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(ArtifactItem, QuestId);
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
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (await GiveQuestItemAsync(player, conn, _itemDao, ArtifactItem, 1, ct))
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return false;
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == SpawnedNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SET_SUCCEED)
            {
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != ArtifactItem) return false;
        if (player.Position.WorldId != TempleWorldId) return false;
        if (player.Quests.Get(QuestId) is null) return false;

        _ = DelayedSpawnAsync(player, conn);
        return true;
    }

    private async Task DelayedSpawnAsync(Player player, GsClientConnection conn)
    {
        await Task.Delay(3000);
        try
        {
            await RemoveQuestItemAsync(player, conn, _itemDao, ArtifactItem, 1, CancellationToken.None);
            var pos = player.Position;
            SpawnQuestNpc(pos.WorldId, pos.InstanceId, SpawnedNpc, pos.X, pos.Y, pos.Z, (byte)pos.Heading);
        }
        catch { }
    }
}
