// Port of Java data/scripts/system/handlers/quest/inggison/_11227EasyAs_4_3_2_1.java.
// Talk to 799076 to start (no item); kill four distinct mobs in a fixed order — 217071, 217070,
// 217069 each advance the var by one, 217068 flips straight to reward; turn in back at 799076.
// Java raw-int dialog id 26 (not DEPOSIT_CHAR_WAREHOUSE's enum meaning here — this npc's dialog UI
// happens to reuse id 26 for its own "select" trigger) is ported as the literal raw check, matching
// Java exactly rather than the named DialogAction enum.
// Skip vs Java: qs.canRepeat() (daily-repeat/cooldown) isn't ported, same simplification as
// _11110KillingTime.cs — completes once per character like a normal quest.
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

namespace Quest.Inggison;

public sealed class _11227EasyAs_4_3_2_1 : QuestHandlerBase
{
    private const int QuestIdConst = 11227;
    private const int TurnInNpc    = 799076;
    private const int Mob4         = 217071;
    private const int Mob3         = 217070;
    private const int Mob2         = 217069;
    private const int Mob1         = 217068;
    private const int SelectDialogId = 26;

    public _11227EasyAs_4_3_2_1(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(TurnInNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Mob1).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Mob2).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Mob3).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Mob4).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (targetId != TurnInNpc) return false;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (env.DialogId == SelectDialogId)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (env.DialogId == SelectDialogId)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 4, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        switch (env.TargetId)
        {
            case Mob4:
            case Mob3:
            case Mob2:
                await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
                return true;
            case Mob1:
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            default:
                return false;
        }
    }
}
