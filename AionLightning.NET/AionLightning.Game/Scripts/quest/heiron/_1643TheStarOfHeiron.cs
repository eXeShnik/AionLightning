// Port of Java data/scripts/system/handlers/quest/heiron/_1643TheStarOfHeiron.java.
// Accepting from 204545 hands over item 182201764; hand it to 204630 (var0->1, spawns 204614
// nearby), then talk to 204614 (var1->2); 204630's SET_SUCCEED flips to REWARD; turn in at 204545.
// Re-entering the world while var==1 resets progress back to 0 (Java safeguard).
// Skip vs Java: the 40s scheduled despawn of the spawned 204614 isn't ported — no NPC lifecycle
// scheduling hook exists yet; the spawn and dialog progression are otherwise faithful.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Heiron;

public sealed class _1643TheStarOfHeiron : QuestHandlerBase
{
    private const int QuestIdConst = 1643;
    private const int StartNpc  = 204545;
    private const int RelayNpc  = 204630;
    private const int SpawnedNpc = 204614;
    private const int StarItem  = 182201764;
    private const int SpawnWorldId = 210040000;

    private readonly IItemDao _itemDao;

    public _1643TheStarOfHeiron(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RelayNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SpawnedNpc).OnTalk.Add(QuestId);
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
                if (dialog == DialogAction.QUEST_ACCEPT_1 && player.Inventory.FindByItemId(StarItem) is null or { Count: 0 })
                {
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, StarItem, 1, ct)) return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == RelayNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO1 && var == 0)
                {
                    entry.SetVar(0, 1);
                    await RemoveQuestItemAsync(player, conn, _itemDao, StarItem, 1, ct);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 0), ct);
                    SpawnQuestNpc(SpawnWorldId, 1, SpawnedNpc, 1591.4327f, 2774.2283f, 127.63001f, 0);
                    return true;
                }
                if (dialog == DialogAction.SET_SUCCEED)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 0), ct);
                    return true;
                }
            }
            else if (targetId == SpawnedNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
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

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is not null && entry.Status == QuestStatus.START && entry.GetVar(0) == 1)
        {
            entry.SetVar(0, 0);
            await UpdateQuestStatusAsync(conn, entry, ct);
        }
        return false;
    }
}
