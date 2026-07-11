// Port of Java data/scripts/system/handlers/quest/eltnen/_1346KillingforCastor.java (Aion Gates).
// Talk to 203966 to start (requires level 27+ per Java's onDialogEvent gate); kill 2 mob groups
// tracked on separate quest vars (var0 up to 4, var1 up to 6); turn in at 203965.
using System.Linq;
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

namespace Quest.Eltnen;

public sealed class _1346KillingforCastor : QuestHandlerBase
{
    private const int QuestIdConst = 1346;
    private const int StartNpc     = 203966;
    private const int TurnInNpc    = 203965;
    private const int MinLevel     = 27;

    private static readonly int[] _var0Mobs = [210872, 210844];
    private static readonly int[] _var1Mobs = [210898, 210878];

    public _1346KillingforCastor(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        foreach (int mob in _var0Mobs) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
        foreach (int mob in _var1Mobs) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int targetId = env.TargetId;
        if (_var0Mobs.Contains(targetId) && entry.GetVar(0) < 4)
        {
            await ChangeQuestStepAsync(conn, entry, 0, entry.GetVar(0) + 1, toReward: false, ct);
            return true;
        }
        if (_var1Mobs.Contains(targetId) && entry.GetVar(1) < 6)
        {
            await ChangeQuestStepAsync(conn, entry, 1, entry.GetVar(1) + 1, toReward: false, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        if (player.Level < MinLevel) return false;

        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId != TurnInNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(1) == 6 && entry.GetVar(0) == 4)
            {
                entry.SetVar(0, entry.GetVar(0) + 1);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT || env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
