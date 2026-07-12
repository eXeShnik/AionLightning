// Port of Java data/scripts/system/handlers/quest/idian_depths/_23617TomboftheElder.java (Evil_dnk),
// quest_data.xml name "Marones Rising". Talk to 730826 to start; accepting spawns a Restless
// Corpse (233040, Java addNewSpawn at player.X-2/Y-2/Z, heading 0) near the player and starts the
// quest immediately (Java calls startQuest then an extra updateQuestStatus - both ported as
// written; the second call is a redundant but harmless status re-broadcast); killing the corpse
// while at var 0 advances to var 1; report to 801547, which flips straight to REWARD without
// moving the var (Java's changeQuestStep(env, 1, 2, true) ignores nextStep on the reward branch);
// turn in at 801547.
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

namespace Quest.IdianDepths;

public sealed class _23617TomboftheElder : QuestHandlerBase
{
    private const int QuestIdConst = 23617;
    private const int StartNpc     = 730826;
    private const int TurnInNpc    = 801547;
    private const int MobId        = 233040;

    public _23617TomboftheElder(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MobId).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
            {
                var pos = player.Position;
                SpawnQuestNpc(pos.WorldId, pos.InstanceId, MobId, pos.X - 2, pos.Y - 2, pos.Z, 0);
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                var started = player.Quests.Get(QuestId);
                if (started is not null) await UpdateQuestStatusAsync(conn, started, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == TurnInNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, MobId, startVar: 0, endVar: 1, ct);
}
