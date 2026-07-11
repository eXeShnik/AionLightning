// Port of Java data/scripts/system/handlers/quest/eltnen/_1430ATeleportationExperiment.java (Xitanium).
// Talk to Onesimus (203919) to start; Sonirim (203337) flips it to REWARD and finishes it.
// Skip vs Java: SETPRO1 calls TeleportService2.teleportTo(player, 220020000, ...) to send the
// player to Heiron - no TeleportService2 exists in this port. The var/status transition (var 0->1,
// REWARD) is kept so the quest stays completable at the same NPC without the teleport (Java's own
// branch has a fall-through bug - updateQuestStatus is called before setStatus(REWARD), so the
// broadcast persists stale status; this port uses ChangeQuestStepAsync to persist var+status
// atomically instead of reproducing that ordering bug).
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

public sealed class _1430ATeleportationExperiment : QuestHandlerBase
{
    private const int QuestIdConst = 1430;
    private const int OnesimusNpc  = 203919;
    private const int SonirimNpc   = 203337;

    public _1430ATeleportationExperiment(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(OnesimusNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(OnesimusNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SonirimNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        int targetId = env.TargetId;
        var entry = env.Player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == OnesimusNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId != SonirimNpc) return false;

        if (entry.Status == QuestStatus.START && entry.GetVar(0) == 0)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                await ChangeQuestStepAsync(conn, entry, 0, entry.GetVar(0) + 1, toReward: true, ct);
                return true;
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4080, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
            {
                entry.SetVar(0, 2);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
