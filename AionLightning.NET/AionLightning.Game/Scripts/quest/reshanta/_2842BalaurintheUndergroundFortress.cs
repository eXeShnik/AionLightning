// Port of Java data/scripts/system/handlers/quest/reshanta/_2842BalaurintheUndergroundFortress.java
// (Hilgert). Same shape as _2841CleansingtheAsteriaChamber: start/turn-in at 266568, kill 38 of any
// of the 8 registered mob types while in the Underground Fortress (worldId 300070000).
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

namespace Quest.Reshanta;

public sealed class _2842BalaurintheUndergroundFortress : QuestHandlerBase
{
    private const int QuestIdConst   = 2842;
    private const int StartNpc       = 266568;
    private const int FortressWorldId = 300070000;
    private const int KillThreshold  = 38;

    private static readonly int[] _mobIds =
        [215447, 214777, 214773, 214781, 214784, 700472, 700474, 700473];

    public _2842BalaurintheUndergroundFortress(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        foreach (int npcId in _mobIds)
            engine.RegisterQuestNpc(npcId).OnKill.Add(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (env.TargetId != StartNpc) return false;

        if (entry is null || entry.Status is QuestStatus.NONE or QuestStatus.COMPLETE)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START) return true;

        if (entry.Status == QuestStatus.REWARD)
        {
            entry.SetVar(0, 0);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is not { Status: QuestStatus.START }) return false;
        if (env.Player.Position.WorldId != FortressWorldId) return false;

        int var = entry.GetVar(0);
        entry.SetVar(0, var + 1);
        if (var >= KillThreshold) entry.Status = QuestStatus.REWARD;
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }
}
