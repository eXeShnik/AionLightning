// Port of Java data/scripts/system/handlers/quest/sanctum/_3212TheMissingCubeCraftsman.java (Cheatkiller).
// Start at 798321; talk 203838 (var0 0->1); talk 798011 (var0 1->2); talk 798337 (var0 2->3) which
// begins an escort of 798337 to fixed coords (505.69/437.69/885.18) — on reach var0 3 -> REWARD, lost
// rolls 3->2; dying or logging out while var0==3 also rolls 3->2; talking 730208 despawns it; turn in at
// 798011. Uses the follow/escort subsystem (StartFollowToCoords + reach/lost hooks).
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

namespace Quest.Sanctum;

public sealed class _3212TheMissingCubeCraftsman : QuestHandlerBase
{
    private const int QuestIdConst = 3212;
    private const int StartNpc     = 798321;
    private const int Step1Npc     = 203838;
    private const int TurninNpc    = 798011;
    private const int EscortNpc    = 798337;
    private const int RemoveObj    = 730208;

    public _3212TheMissingCubeCraftsman(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService) { }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Step1Npc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurninNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EscortNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RemoveObj).OnTalk.Add(QuestId);
        RegisterOnReachTarget(engine);
        RegisterOnLostTarget(engine);
        RegisterOnDie(engine);
        RegisterOnLogOut(engine);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == Step1Npc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }

            if (targetId == TurninNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }

            if (targetId == EscortNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3)
                {
                    StartFollowToCoords(env, conn, (Npc)env.Target!, 505.69427f, 437.69382f, 885.1844f);
                    // note: Java setWalkerId("4212") + WalkManager.startWalking + START_EMOTE2 broadcast dropped — cosmetic escort walk-AI, follow subsystem drives movement
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                }
                return false;
            }

            if (targetId == RemoveObj)
            {
                // note: Java npc.getController().delete() (despawn interactable) dropped — cosmetic
                return true;
            }

            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurninNpc)
        {
            if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) == 3)
        {
            entry.SetVar(0, 2);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnLogOutAsync(QuestEnv env, GsClientConnection? conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) == 3)
        {
            entry.SetVar(0, 2);
            if (conn is not null) await UpdateQuestStatusAsync(conn, entry, ct);
            else await QuestDao.UpsertAsync(player.ObjectId, entry, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnNpcReachTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 3) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: true, ct);
        return true;
    }

    public override async ValueTask<bool> OnNpcLostTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 3) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
        return true;
    }
}
