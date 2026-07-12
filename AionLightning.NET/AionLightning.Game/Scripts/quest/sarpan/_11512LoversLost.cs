// Port of Java data/scripts/system/handlers/quest/sarpan/_11512LoversLost.java (Cheatkiller).
// Talk to Kaidan Kiyas (205989) to start; use the Mysterious Grave (730467, var 1->2) which spawns
// a mob (218650) at its position; kill it to flip to REWARD; turn in at Yakumo Sadaayo (205746).
// Skip vs Java: SETPRO2 also calls npc.getController().onDelete() on the Mysterious Grave after
// spawning the mob - no NPC despawn API is exposed to hand-written quest scripts in this port, so
// the placeholder object is left standing. Doesn't affect completability (the spawned mob is the
// actual objective).
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

namespace Quest.Sarpan;

public sealed class _11512LoversLost : QuestHandlerBase
{
    private const int QuestIdConst = 11512;
    private const int KaidanKiyasNpc = 205989;
    private const int YakumoSadaayoNpc = 205746;
    private const int MysteriousGraveNpc = 730467;
    private const int MobId = 218650;

    public _11512LoversLost(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(KaidanKiyasNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(YakumoSadaayoNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KaidanKiyasNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MysteriousGraveNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MobId).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, MobId, startVar: 2, reward: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != KaidanKiyasNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == KaidanKiyasNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == MysteriousGraveNpc)
            {
                if (dialog == DialogAction.USE_OBJECT && entry.GetVar(0) == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2 && env.Target is not null)
                {
                    var pos = env.Target.Position;
                    SpawnQuestNpc(pos.WorldId, pos.InstanceId, MobId, pos.X, pos.Y, pos.Z, (byte)pos.Heading);
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                }
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == YakumoSadaayoNpc)
        {
            if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
