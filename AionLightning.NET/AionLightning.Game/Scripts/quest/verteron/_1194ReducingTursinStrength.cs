// Port of Java data/scripts/system/handlers/quest/verteron/_1194ReducingTursinStrength.java
// (Balthazar). Talk to Santenius (203098) to start; kill 210185/210186 x9 (var 1->10,
// reward-flip at 10); turn in at Santenius.
// Skip vs Java: entering the Tursin Garrison zone (TURSIN_GARRISON_210030000) is what arms the
// kill counter (var 0->1) in Java — no zone-shape system exists in C#, so this port arms the
// counter immediately on quest accept instead (var starts at 1, not 0). Same net effect for
// gameplay: the player must still travel to the garrison to find the kill targets.
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

namespace Quest.Verteron;

public sealed class _1194ReducingTursinStrength : QuestHandlerBase
{
    private const int QuestIdConst  = 1194;
    private const int SanteniusNpc  = 203098;
    private static readonly int[] _mobs = [210185, 210186];

    public _1194ReducingTursinStrength(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(SanteniusNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(SanteniusNpc).OnTalk.Add(QuestId);
        foreach (int mob in _mobs)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry is null)
        {
            if (targetId != SanteniusNpc) return false;
            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);

            if (!await SendQuestStartDialogAsync(env, conn, ct)) return false;
            var created = player.Quests.Get(QuestId);
            if (created is not null) await ChangeQuestStepAsync(conn, created, 0, 1, toReward: false, ct);
            return true;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == SanteniusNpc)
        {
            switch (DialogActionLookup.FromId(env.DialogId))
            {
                case DialogAction.USE_OBJECT:
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                case DialogAction.SELECT_QUEST_REWARD:
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                default:
                    return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || !_mobs.Contains(env.TargetId)) return false;

        int var = entry.GetVar(0);
        if (var is < 1 or >= 10) return false;

        if (var == 9)
            await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
        else
            await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
        return true;
    }
}
