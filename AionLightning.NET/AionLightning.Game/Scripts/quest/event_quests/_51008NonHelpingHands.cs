// Port of Java data/scripts/system/handlers/quest/event_quests/_51008NonHelpingHands.java (Alcapwnd).
// Start/turn-in at 831039. When NPC 219291 first aggros the player (onAddAggroList), the aggroed NPC
// escorts the player toward zone Q51008 (Java defaultStartFollowEvent → StartFollowToZone). On reach,
// var0 advances 0->1 (first segment) then 1->2 + REWARD (second segment). Only reach-target is
// registered (no lost-target), faithful to Java.
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

namespace Quest.EventQuests;

public sealed class _51008NonHelpingHands : QuestHandlerBase
{
    private const int QuestIdConst = 51008;
    private const int StartNpc     = 831039;
    private const int TalkNpc      = 831037;
    private const int AggroNpc     = 219291;
    private const string FollowZone = "Q51008";

    public _51008NonHelpingHands(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService) { }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        // note: Java registered 831037 onTalk twice (redundant); it has no dialog case (dead registration, kept faithful).
        engine.RegisterQuestNpc(TalkNpc).OnTalk.Add(QuestId);
        RegisterOnAddAggroList(engine, AggroNpc);
        RegisterOnReachTarget(engine);
    }

    public override ValueTask<bool> OnAddAggroListAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return ValueTask.FromResult(false);

        int var = entry.GetVar(0);
        if (var == 0 || var == 1)
        {
            // Java defaultStartFollowEvent(env, aggroer, Q51008, var, var): follower is the NPC that aggroed.
            // note: the var==1 path's cosmetic defaultCloseDialog(1,1) (no state change) is dropped.
            return ValueTask.FromResult(StartFollowToZone(env, conn, (Npc)env.Target!, FollowZone));
        }
        return ValueTask.FromResult(false);
    }

    public override async ValueTask<bool> OnNpcReachTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var == 0)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
            return true;
        }
        if (var == 1)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: true, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }
}
