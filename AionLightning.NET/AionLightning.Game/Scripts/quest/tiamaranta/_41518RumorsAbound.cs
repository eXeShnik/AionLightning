// Port of Java data/scripts/system/handlers/quest/tiamaranta/_41518RumorsAbound.java (Cheatkiller).
// Talk to 205938 to accept (grants quest item 182212588); interacting with 701260 spawns two mobs
// (218731/218732) beside the player and advances var 0->1; entering ARACHI_FLOATING_ISLAND_600030000
// advances 1->2; entering SUSPICIOUS_VILLAGE_ENTRANCE_600030000 at var 2 flips to REWARD; turn in at 205938.
// Skip vs Java: qe.registerCanAct(701260) has no equivalent in this port (no CanAct hook / registration) -
// 701260 is registered as a normal OnTalk quest npc instead, matching the tiamaranta._41519KillTheShepherd
// precedent for interactable quest objects. Purely a registration detail; the talk still dispatches.
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

namespace Quest.Tiamaranta;

public sealed class _41518RumorsAbound : QuestHandlerBase
{
    private const int QuestIdConst = 41518;
    private const int StartNpc     = 205938;
    private const int ObjectNpc    = 701260;
    private const int MobA         = 218731;
    private const int MobB         = 218732;
    private const int ItemId       = 182212588;
    private const string VillageZone = "SUSPICIOUS_VILLAGE_ENTRANCE_600030000";
    private const string IslandZone  = "ARACHI_FLOATING_ISLAND_600030000";

    private readonly IItemDao _itemDao;

    public _41518RumorsAbound(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ObjectNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, VillageZone);
        RegisterOnEnterZone(engine, IslandZone);
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
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
                await GiveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == ObjectNpc)
        {
            var pos = player.Position;
            SpawnQuestNpc(pos.WorldId, pos.InstanceId, MobA, pos.X + 2, pos.Y + 2, pos.Z, 0);
            SpawnQuestNpc(pos.WorldId, pos.InstanceId, MobB, pos.X - 2, pos.Y - 2, pos.Z, 0);
            await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
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

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        int var = entry.GetVar(0);

        if (zoneName == VillageZone && var == 2)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: true, ct);
            return true;
        }
        if (zoneName == IslandZone && var == 1)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
            return true;
        }
        return false;
    }
}
