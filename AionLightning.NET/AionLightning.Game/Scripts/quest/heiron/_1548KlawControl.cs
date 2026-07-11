// Port of Java data/scripts/system/handlers/quest/heiron/_1548KlawControl.java.
// Talk to Senea (204583) to start; kill Klaw Spawn (700209) 5 times (var 0->5, REWARD on the
// 5th); turn in at Senea.
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

namespace Quest.Heiron;

public sealed class _1548KlawControl : QuestHandlerBase
{
    private const int QuestIdConst = 1548;
    private const int SeneaNpc     = 204583;
    private const int KlawSpawnNpc = 700209;

    public _1548KlawControl(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(SeneaNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(SeneaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KlawSpawnNpc).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (env.TargetId != SeneaNpc) return false;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, env.Target?.ObjectId ?? 0, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.TargetId != KlawSpawnNpc) return false;

        int var = entry.GetVar(0);
        if (var is >= 0 and < 4)
        {
            await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
            return true;
        }
        if (var == 4)
        {
            await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: true, ct);
            return true;
        }
        return false;
    }
}
