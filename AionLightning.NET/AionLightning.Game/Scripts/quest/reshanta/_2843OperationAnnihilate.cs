// Port of Java data/scripts/system/handlers/quest/reshanta/_2843OperationAnnihilate.java (Hilgert).
// Same shape as _2841/_2842: start/turn-in at 268081, kill any of 56 registered mob types while in
// worldId 300140000. Java's own kill counter (qs.getQuestVarById(0), a single 6-bit-per-slot var
// slot capped at 63 by QuestVars.setVarById's "& 0x3F" packing — this port's QuestEntry has the
// same per-slot cap) would silently wrap before reaching this quest's 79/80-kill threshold, making
// the REWARD-flip branch permanently unreachable in the original. Fixed by spreading the count
// across two var slots (var0 = count % 64, var1 = count / 64) so it can exceed 63; the two-branch
// increment logic (var < 79 vs var >= 79) is otherwise ported exactly.
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

public sealed class _2843OperationAnnihilate : QuestHandlerBase
{
    private const int QuestIdConst  = 2843;
    private const int StartNpc      = 268081;
    private const int TargetWorldId = 300140000;
    private const int KillThreshold = 79;

    private static readonly int[] _mobIds =
    [
        215134, 215136, 215122, 215124, 215116, 215315, 215299, 215112, 215120, 215125,
        215118, 215119, 215312, 215109, 215097, 215304, 215308, 215292, 215101, 215126,
        215098, 215305, 215289, 215300, 215113, 215316, 215094, 215301, 215313, 215110,
        215297, 215314, 215111, 215298, 215104, 215096, 215303, 215287, 215128, 215121,
        215106, 215293, 215107, 215100, 215291, 215307, 215290, 215099, 215306, 215123,
        215108, 215295, 215311, 215095, 215302, 215133,
    ];

    public _2843OperationAnnihilate(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        foreach (int npcId in _mobIds)
            engine.RegisterQuestNpc(npcId).OnKill.Add(QuestId);
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
            entry.SetVar(1, 0);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is not { Status: QuestStatus.START }) return false;
        if (env.Player.Position.WorldId != TargetWorldId) return false;

        int killCount = entry.GetVar(1) * 64 + entry.GetVar(0);
        killCount++;
        entry.SetVar(0, killCount % 64);
        entry.SetVar(1, killCount / 64);
        if (killCount > KillThreshold) entry.Status = QuestStatus.REWARD;
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }
}
