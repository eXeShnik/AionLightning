// Port of Java data/scripts/system/handlers/quest/katalam/_20083NotBushNorBramble.java (Cheatkiller).
// Asmodian mirror of _10083AReianRecord (shares the same 730709/730710 world objects). Preceding
// quest 20082 unlocks this on level-up (20082 itself is deferred — zone-based). Talk chain
// 800537 -> 800539 -> 800544 (also turns in); 730709 spawns a pair of quest npcs beside itself
// (Batch 0.2 SpawnQuestNpc primitive), 730710 hands over the record item.
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

namespace Quest.Katalam;

public sealed class _20083NotBushNorBramble : QuestHandlerBase
{
    private const int QuestIdConst = 20083;
    private const int FirstNpc     = 800537;
    private const int SecondNpc    = 800539;
    private const int TurnInNpc    = 800544;
    private const int SpawnerNpc   = 730709;
    private const int GiverNpc     = 730710;
    private const int RecordItemId = 182215236;

    private readonly IItemDao _itemDao;

    public _20083NotBushNorBramble(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npcId in new[] { FirstNpc, SecondNpc, TurnInNpc, SpawnerNpc, GiverNpc })
            engine.RegisterQuestNpc(npcId).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 20082, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is not null && entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == FirstNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == SecondNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == TurnInNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 2)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3)
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                return false;
            }
            if (targetId == SpawnerNpc)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 3)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO5 && env.Target is not null)
                {
                    var pos = env.Target.Position;
                    SpawnQuestNpc(pos.WorldId, pos.InstanceId, 230392, pos.X + 2, pos.Y - 2, pos.Z, 0);
                    SpawnQuestNpc(pos.WorldId, pos.InstanceId, 230391, pos.X - 2, pos.Y + 2, pos.Z, 0);
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                }
                return false;
            }
            if (targetId == GiverNpc)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 4)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO4)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, RecordItemId, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 4, 4, reward: true, sameNpc: false, ct);
                }
                return false;
            }
        }
        else if (entry is not null && entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, RecordItemId, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
