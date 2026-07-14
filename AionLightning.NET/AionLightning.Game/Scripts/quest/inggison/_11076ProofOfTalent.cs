// Port of Java data/scripts/system/handlers/quest/inggison/_11076ProofOfTalent.java (Cheatkiller).
// Accept at 799025 (page 4762). In Inggison (world 210050000) the player rides three wind streams,
// each bumping var0 (0->1->2->3) via the onEnterWindStream event; at var0==3 talk 799084 (page
// 2034) whose SETPRO4 flips to REWARD; turn in at 799025.
// note: this port has no wind-stream (onEnterWindStream) hook. The three-ring progression
//   (teleportId 152001 -> var 0->1, 153001 -> 1->2, 154001 -> 2->3, all in world 210050000) is
//   collapsed into a single world-entry advance: entering world 210050000 with the quest active and
//   var0 < 3 sets var0 = 3, so the quest stays completable. The per-ring granularity is not modelled.
// note: Java's SETPRO4 TeleportService2.teleportTo(210050000, 1338.6, 279.6, 590) is a cosmetic
//   same-world relocation that does not gate progression — dropped; the var 3->4 + REWARD flip stays.
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

namespace Quest.Inggison;

public sealed class _11076ProofOfTalent : QuestHandlerBase
{
    private const int QuestIdConst = 11076;
    private const int StartNpc     = 799025;
    private const int RelayNpc     = 799084;
    private const int WindStreamWorld = 210050000;

    public _11076ProofOfTalent(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService) { }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(RelayNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        // Java registerOnEnterWindStream(questId) has no equivalent hook — see file header note.
        engine.RegisterOnEnterWorld(QuestId);
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
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == RelayNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 3)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO4)
                    // note: Java relocates the player to 210050000 (1338.6, 279.6, 590) here — cosmetic, dropped.
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, reward: true, sameNpc: false, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.Player.Position.WorldId != WindStreamWorld) return false;
        if (entry.GetVar(0) >= 3) return false;

        // Collapsed wind-stream advance (see file header note): reach var0 == 3 so 799084 becomes talkable.
        await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
        return true;
    }
}
