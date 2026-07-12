// Port of Java data/scripts/system/handlers/quest/reshanta/_2759TenaciousGuardian.java (vlog).
// Start/turn-in at 264769; kill one each of 3 mob types (278588/278589/278590, any order) to bump
// var0 from 0 to 3, then flips to REWARD via SELECT_QUEST_REWARD.
// Fixed a latent Java bug: the original tracked "already credited this mob type" via a single
// List&lt;Integer&gt; field on the (singleton, shared-across-all-players) quest handler instance —
// once any player killed e.g. 278588, that entry stayed in the list forever and no other player
// could ever get credit for killing it again, effectively softlocking the quest server-wide after
// its first use. Fixed by tracking per-player credited-mob flags in quest var slot 1 as a bitmask
// (bit per mob type) instead of shared mutable state.
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

public sealed class _2759TenaciousGuardian : QuestHandlerBase
{
    private const int QuestIdConst = 2759;
    private const int StartNpc     = 264769;

    private static readonly (int NpcId, int Bit)[] _mobs = [(278588, 1), (278589, 2), (278590, 4)];

    public _2759TenaciousGuardian(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        foreach (var (npcId, _) in _mobs)
            engine.RegisterQuestNpc(npcId).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId != StartNpc) return false;

        if (entry is null)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (dialog == DialogAction.QUEST_SELECT && var == 3)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: true, ct);
                entry.SetVar(1, 0);
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is not { Status: QuestStatus.START }) return false;

        int var = entry.GetVar(0);
        if (var >= 3) return false;

        foreach (var (npcId, bit) in _mobs)
        {
            if (env.TargetId != npcId) continue;

            int killedMask = entry.GetVar(1);
            if ((killedMask & bit) != 0) return false;
            entry.SetVar(1, killedMask | bit);

            return await DefaultOnKillEventAsync(env, conn, npcId, var, var + 1, ct);
        }

        return false;
    }
}
