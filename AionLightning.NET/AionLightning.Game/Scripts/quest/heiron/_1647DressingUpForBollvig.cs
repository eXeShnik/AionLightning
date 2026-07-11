// Port of Java data/scripts/system/handlers/quest/heiron/_1647DressingUpForBollvig.java.
// Talk to Zetus (790019) to start (receives Myanee's Flute); use the Suspicious Stone Statue
// (700272) while wearing the Stenon Blouse (110100150) and Stenon Skirt (113100144) and holding
// the flute to play movie 199 and flip to REWARD; turn in at Zetus. On movie 199 finishing,
// spawns 3 Klaw Cultists (204635) near the player (Java QuestService.addNewSpawn parity via the
// Batch 0.2 SpawnQuestNpc primitive).
using System.Linq;
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

public sealed class _1647DressingUpForBollvig : QuestHandlerBase
{
    private const int QuestIdConst = 1647;
    private const int ZetusNpc     = 790019;
    private const int StatueObject = 700272;
    private const int FluteItemId  = 182201783;
    private const int BlouseItemId = 110100150;
    private const int SkirtItemId  = 113100144;
    private const int CultistNpc   = 204635;
    private const int MovieId      = 199;

    private readonly IItemDao _itemDao;

    public _1647DressingUpForBollvig(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(ZetusNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(ZetusNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StatueObject).OnTalk.Add(QuestId);
        engine.RegisterOnQuestMovieEnd(MovieId, QuestId);
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
            if (targetId != ZetusNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);

            if (dialog == DialogAction.QUEST_ACCEPT_1)
            {
                if (await StartMissionAsync(conn, player, QuestStatus.START, ct))
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, FluteItemId, 1, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
                }
                return false;
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == StatueObject && dialog == DialogAction.USE_OBJECT)
        {
            bool wearingBlouse = player.Inventory.All.Any(i => i.ItemId == BlouseItemId && i.IsEquipped);
            bool wearingSkirt  = player.Inventory.All.Any(i => i.ItemId == SkirtItemId && i.IsEquipped);
            long fluteCount    = player.Inventory.FindByItemId(FluteItemId)?.Count ?? 0;

            if (wearingBlouse && wearingSkirt && fluteCount > 0)
            {
                await PlayQuestMovieAsync(conn, player, MovieId, ct);
                return await UseQuestObjectAsync(env, conn, 0, 0, reward: true, dieObject: false, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == ZetusNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.REWARD || movieId != MovieId)
            return ValueTask.FromResult(false);

        var pos = player.Position;
        SpawnQuestNpc(pos.WorldId, pos.InstanceId, CultistNpc, pos.X, pos.Y, pos.Z, 0);
        SpawnQuestNpc(pos.WorldId, pos.InstanceId, CultistNpc, pos.X + 2, pos.Y - 2, pos.Z, 0);
        SpawnQuestNpc(pos.WorldId, pos.InstanceId, CultistNpc, pos.X - 2, pos.Y + 2, pos.Z, 0);
        return ValueTask.FromResult(true);
    }
}
