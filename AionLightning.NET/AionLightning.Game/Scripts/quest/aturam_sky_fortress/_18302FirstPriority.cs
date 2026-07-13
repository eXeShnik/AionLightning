// Port of Java data/scripts/system/handlers/quest/aturam_sky_fortress/_18302FirstPriority.java (Luzien).
// Talk to 799530 to start (plays movie 468 on accept); kill 5 mobs (700981-700985, var0 0->5); at
// var0==5 talk to 730375 (SET_SUCCEED flips REWARD); turn in at 799530, removing item 182212101.
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

namespace Quest.AturamSkyFortress;

public sealed class _18302FirstPriority : QuestHandlerBase
{
    private const int QuestIdConst = 18302;
    private const int StartNpc     = 799530;
    private const int TurnInNpc    = 730375;
    private const int RewardItem   = 182212101;
    private static readonly int[] MobIds = [700981, 700982, 700983, 700984, 700985];

    private readonly IItemDao _itemDao;

    public _18302FirstPriority(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        foreach (int mobId in MobIds)
            engine.RegisterQuestNpc(mobId).OnKill.Add(QuestId);
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
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
                {
                    await PlayQuestMovieAsync(conn, player, 468, ct);
                    return await SendQuestStartDialogAsync(env, conn, ct);
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }

        if (entry is null) return false;

        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == TurnInNpc)
            {
                if (var == 5)
                {
                    if (dialog == DialogAction.USE_OBJECT)
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (dialog == DialogAction.SET_SUCCEED)
                    {
                        entry.Status = QuestStatus.REWARD;
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await CloseDialogWindowAsync(conn, targetObjId, ct);
                    }
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                }
            }
            else if (targetId == StartNpc)
            {
                return await SendQuestDialogAsync(conn, targetObjId, 1004, ct);
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, RewardItem, 1, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var > 4) return false;

        int targetId = env.TargetId;
        foreach (int mobId in MobIds)
        {
            if (targetId == mobId)
            {
                await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
                return true;
            }
        }
        return false;
    }
}
