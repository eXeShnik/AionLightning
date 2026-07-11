// Port of Java data/scripts/system/handlers/quest/ishalgen/_2136TheLostAxe.java.
// Item-use start (Lost Axe 182203130); use object 700146 to play movie 59 and spawn Rhoo (790009)
// via the Batch 0.2 SpawnQuestNpc primitive; talk to Rhoo, choose a reward (SETPRO1/2 → dialog
// 6/5), turn in. Uses the OnItemUse trigger + SpawnQuestNpc.
// Skip vs Java: the 10s scheduled despawn of Rhoo after turn-in and the SM_ITEM_USAGE_ANIMATION
// cosmetic are omitted (harmless — Rhoo simply persists).
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Ishalgen;

public sealed class _2136TheLostAxe : QuestHandlerBase
{
    private const int QuestIdConst = 2136;
    private const int RhooNpc      = 790009;
    private const int ObjectNpc    = 700146;
    private const int AxeItemId    = 182203130;

    private readonly IItemDao _itemDao;

    public _2136TheLostAxe(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(AxeItemId, QuestId);
        engine.RegisterQuestNpc(ObjectNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RhooNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != AxeItemId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (await StartMissionAsync(conn, player, QuestStatus.START, ct))
                await conn.SendAsync(new SM_DIALOG_WINDOW(0, 4, QuestId), ct);
        }
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        // Item-sourced accept
        if (targetId == 0 && (entry is null || entry.Status == QuestStatus.NONE))
        {
            if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
            await conn.SendAsync(new SM_DIALOG_WINDOW(0, 0), ct);
            return true;
        }

        if (entry is null) return false;
        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.REWARD && targetId == RhooNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        if (entry.Status != QuestStatus.START) return false;

        if (targetId == RhooNpc && var == 1)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                case DialogAction.SETPRO1:
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, AxeItemId, 1, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 6, ct);
                case DialogAction.SETPRO2:
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, AxeItemId, 1, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                default:
                    return false;
            }
        }

        if (targetId == ObjectNpc && dialog == DialogAction.USE_OBJECT && var == 0)
        {
            await PlayQuestMovieAsync(conn, player, 59, ct);
            entry.SetVar(0, 1);
            await UpdateQuestStatusAsync(conn, entry, ct);
            // Java addNewSpawn(220010000, instance, 790009, ...) — Rhoo, the axe holder
            SpawnQuestNpc(220010000, player.Position.InstanceId, RhooNpc, 1088.5f, 2371.8f, 258.375f, 87);
            return true;
        }
        return false;
    }
}
