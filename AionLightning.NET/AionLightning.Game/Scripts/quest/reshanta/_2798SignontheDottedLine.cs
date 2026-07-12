// Port of Java data/scripts/system/handlers/quest/reshanta/_2798SignontheDottedLine.java.
// Start at 279007 (gives item 182205646 on accept); a 9-step relay chain, each step var-gated and
// advanced via defaultCloseDialog (263569 -> 263267 -> 264769 -> 271054 -> 266554 -> 270152 ->
// 269252 -> 268052 -> 260236, the last flipping to REWARD); turn in back at 279007. Java's
// sendQuestNoneDialog/sendQuestRewardDialog helpers aren't ported to the shared base (only this
// quest uses them), so they're reproduced here as small private helpers composed from existing
// QuestHandlerBase primitives. Java's qs.canRepeat() gate is approximated as "no active entry",
// matching the rest of this port (see QuestEngine.ComputeNearbyQuests).
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

namespace Quest.Reshanta;

public sealed class _2798SignontheDottedLine : QuestHandlerBase
{
    private const int QuestIdConst = 2798;
    private const int StartNpc     = 279007;
    private const int StartItemId  = 182205646;

    private static readonly (int Npc, int SelectPage, DialogAction Setpro)[] _steps =
    [
        (263569, 1011, DialogAction.SETPRO1),
        (263267, 1352, DialogAction.SETPRO2),
        (264769, 1693, DialogAction.SETPRO3),
        (271054, 2034, DialogAction.SETPRO4),
        (266554, 2375, DialogAction.SETPRO5),
        (270152, 2716, DialogAction.SETPRO6),
        (269252, 3057, DialogAction.SETPRO7),
        (268052, 3398, DialogAction.SETPRO8),
        (260236, 3739, DialogAction.SET_SUCCEED),
    ];

    private readonly IItemDao _itemDao;

    public _2798SignontheDottedLine(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        foreach (var (npc, _, _) in _steps)
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        if (await SendQuestNoneDialogAsync(env, conn, ct)) return true;

        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);
        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            for (int i = 0; i < _steps.Length; i++)
            {
                var (npc, selectPage, setpro) = _steps[i];
                if (targetId != npc || var != i) continue;

                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, selectPage, ct);
                if (dialog == setpro)
                {
                    bool reward = i == _steps.Length - 1;
                    return await DefaultCloseDialogAsync(env, conn, i, i + 1, reward, sameNpc: false, ct);
                }
                break;
            }
        }

        return await SendQuestRewardDialogAsync(env, conn, ct);
    }

    /// <summary>Java sendQuestNoneDialog(env, 279007, 4762, itemId, itemCount): shows the accept
    /// dialog or gives the starting item and starts the quest when no entry exists yet.</summary>
    private async ValueTask<bool> SendQuestNoneDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        if (player.Quests.Get(QuestId) is not null) return false;
        if (env.TargetId != StartNpc) return false;

        int targetObjId = env.Target?.ObjectId ?? 0;
        if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
            return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);

        if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
        {
            if (await GiveQuestItemAsync(player, conn, _itemDao, StartItemId, 1, ct))
                return await SendQuestStartDialogAsync(env, conn, ct);
            return true;
        }
        return await SendQuestStartDialogAsync(env, conn, ct);
    }

    /// <summary>Java sendQuestRewardDialog(env, 279007, 10002): the turn-in fallback tried after
    /// every relay-step check fails to match.</summary>
    private async ValueTask<bool> SendQuestRewardDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is not { Status: QuestStatus.REWARD } || env.TargetId != StartNpc) return false;

        int targetObjId = env.Target?.ObjectId ?? 0;
        if (DialogActionLookup.FromId(env.DialogId) == DialogAction.USE_OBJECT)
            return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
        return await SendQuestEndDialogAsync(env, conn, ct);
    }
}
