// Port of Java data/scripts/system/handlers/quest/reshanta/_2841CleansingtheAsteriaChamber.java
// (Hilgert). Start/turn-in at 271068; kill 43 of any of the 9 registered mob types while in the
// Asteria Chamber (worldId 300050000) to flip to REWARD. registerOnEnterWorld is called (matching
// Java) but Java never overrides onEnterWorldEvent for this quest either, so it is inert here too.
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

public sealed class _2841CleansingtheAsteriaChamber : QuestHandlerBase
{
    private const int QuestIdConst  = 2841;
    private const int StartNpc      = 271068;
    private const int ChamberWorldId = 300050000;
    private const int KillThreshold = 43;

    private static readonly int[] _mobIds =
        [214762, 214755, 214752, 214754, 215441, 214758, 214766, 214753, 215444];

    public _2841CleansingtheAsteriaChamber(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
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
        if (env.Player.Position.WorldId != ChamberWorldId) return false;

        int var = entry.GetVar(0);
        entry.SetVar(0, var + 1);
        if (var >= KillThreshold) entry.Status = QuestStatus.REWARD;
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }
}
