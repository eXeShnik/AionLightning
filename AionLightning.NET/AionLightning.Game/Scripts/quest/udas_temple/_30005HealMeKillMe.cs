// Port of Java data/scripts/system/handlers/quest/udas_temple/_30005HealMeKillMe.java (vlog).
// Talk to Honeus (799029) to start; kill 24 of {215857, 215814, 215858, 215815} in any combination
// (var 0->24, one per kill), the 24th kill flips straight to REWARD; turn in at Honeus.
// Skip vs Java: qs.canRepeat() (daily-reset re-entry allowing this quest to restart after
// completion) isn't ported, matching every other kill quest already in this codebase (e.g.
// danaria's _13350TheyRunFromYou) - once COMPLETE the quest cannot restart here.
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

namespace Quest.UdasTemple;

public sealed class _30005HealMeKillMe : QuestHandlerBase
{
    private const int QuestIdConst = 30005;
    private const int HoneusNpc    = 799029;

    private static readonly int[] _mobs = [215857, 215814, 215858, 215815];

    public _30005HealMeKillMe(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(HoneusNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(HoneusNpc).OnTalk.Add(QuestId);
        foreach (int mob in _mobs)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == HoneusNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == HoneusNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return ValueTask.FromResult(false);

        int var = entry.GetVar(0);
        if (var is >= 0 and < 24)
            return DefaultOnKillEventAsync(env, conn, _mobs, 0, 24, ct);
        if (var == 24)
            return DefaultOnKillEventAsync(env, conn, _mobs, 24, true, ct);
        return ValueTask.FromResult(false);
    }
}
