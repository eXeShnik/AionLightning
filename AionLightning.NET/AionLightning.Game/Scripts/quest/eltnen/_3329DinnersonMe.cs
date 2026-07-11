// Port of Java data/scripts/system/handlers/quest/eltnen/_3329DinnersonMe.java (Ritsu).
// Talk to 203909 to start; kill 2 mob groups tracked on separate quest vars (var0 up to 11, var1
// up to 17 - no reward-flip gate on the kills, faithful to Java); turn in at 203956.
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

public sealed class _3329DinnersonMe : QuestHandlerBase
{
    private const int QuestIdConst = 3329;
    private const int StartNpc     = 203909;
    private const int TurnInNpc    = 203956;

    private static readonly int[] _var0Mobs = [210887, 210912];
    private static readonly int[] _var1Mobs = [210914, 210932];

    public _3329DinnersonMe(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
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
        if (_var0Mobs.Contains(targetId) && entry.GetVar(0) < 11)
        {
            await ChangeQuestStepAsync(conn, entry, 0, entry.GetVar(0) + 1, toReward: false, ct);
            return true;
        }
        if (_var1Mobs.Contains(targetId) && entry.GetVar(1) < 17)
        {
            await ChangeQuestStepAsync(conn, entry, 1, entry.GetVar(1) + 1, toReward: false, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
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

        if (entry.Status == QuestStatus.START && targetId == TurnInNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                case DialogAction.SELECT_QUEST_REWARD:
                    return await SendQuestEndDialogAsync(env, conn, ct);
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
