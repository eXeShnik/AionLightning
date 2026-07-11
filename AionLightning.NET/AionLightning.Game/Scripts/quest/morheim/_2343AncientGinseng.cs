// Port of Java data/scripts/system/handlers/quest/morheim/_2343AncientGinseng.java.
// Using the ginseng root item (182204134) offers to start the quest; accepting and then talking
// to Elder Ronkarr (700243) spawns the reward npc (Java addNewSpawn, guarded there by "npc not
// already in the world" — no WorldMapInstance NPC lookup exists in this port, so this always
// spawns; harmless, matches the Batch 0.2 SpawnQuestNpc convention), removes the root and finishes
// with the default reward tier. Java's registerCanAct gate has no port-side equivalent and is
// omitted (OnTalk registration already scopes the dialog dispatch to this npc).
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

namespace Quest.Morheim;

public sealed class _2343AncientGinseng : QuestHandlerBase
{
    private const int QuestIdConst = 2343;
    private const int ElderNpc     = 700243;
    private const int RewardNpcId  = 212814;
    private const int GinsengItem  = 182204134;

    private readonly IItemDao _itemDao;

    public _2343AncientGinseng(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(GinsengItem, QuestId);
        engine.RegisterQuestNpc(ElderNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != GinsengItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is not null) return false;

        await conn.SendAsync(new SM_DIALOG_WINDOW(0, 4, QuestId), ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;

        if (entry is null)
        {
            if (targetId != 0 || env.DialogId != (int)DialogAction.QUEST_ACCEPT_1) return false;
            await StartMissionAsync(conn, player, QuestStatus.START, ct);
            return await CloseDialogWindowAsync(conn, 0, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == ElderNpc)
        {
            var pos = player.Position;
            SpawnQuestNpc(pos.WorldId, pos.InstanceId, RewardNpcId, 1008.36f, 481.88f, 509.3f, 60);
            await RemoveQuestItemAsync(player, conn, _itemDao, GinsengItem, 1, ct);
            entry.Status = QuestStatus.REWARD;
            await FinishQuestAsync(conn, player, 0, ct);
            return true;
        }
        return false;
    }
}
