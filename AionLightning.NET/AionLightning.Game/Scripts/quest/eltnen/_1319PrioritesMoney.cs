// Port of Java data/scripts/system/handlers/quest/eltnen/_1319PrioritesMoney.java (Xitanium).
// Talk to Priorite (203908) to start; a simple relay chain through eight NPCs (Krato, Hebestis,
// Benos, Diokles, Tuskeos, Girrinerk, Shaoranyerk, Arnesonerk) advances var 0 through 7, then flips
// to REWARD; turn in at Priorite. Java's REWARD branch doesn't re-check the target NPC id (any
// registered NPC's dialog while in REWARD routes through the same completion logic) - kept as-is,
// since only Priorite is shown a reward-capable dialog by the client in practice.
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

public sealed class _1319PrioritesMoney : QuestHandlerBase
{
    private const int QuestIdConst = 1319;
    private const int PrioriteNpc  = 203908;
    private const int KratoNpc     = 203923;
    private const int HebestisNpc  = 203910;
    private const int BenosNpc     = 203906;
    private const int DioklesNpc   = 203915;
    private const int TuskeosNpc   = 203907;
    private const int GirrinerkNpc = 798050;
    private const int ShaoranyerkNpc = 798049;
    private const int ArnesonerkNpc  = 205240;

    public _1319PrioritesMoney(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(PrioriteNpc).OnQuestStart.Add(QuestId);
        foreach (int npc in new[] { PrioriteNpc, KratoNpc, HebestisNpc, BenosNpc, DioklesNpc, TuskeosNpc, GirrinerkNpc, ShaoranyerkNpc, ArnesonerkNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
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
            if (targetId != PrioriteNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4080, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
            {
                entry.SetVar(0, 8);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        if (entry.Status != QuestStatus.START) return false;

        if (targetId == KratoNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return false;
        }
        if (targetId == HebestisNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            return false;
        }
        if (targetId == BenosNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            if (dialog == DialogAction.SETPRO3) return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
            return false;
        }
        if (targetId == DioklesNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SETPRO4) return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
            return false;
        }
        if (targetId == TuskeosNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
            if (dialog == DialogAction.SETPRO5) return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
            return false;
        }
        if (targetId == GirrinerkNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
            if (dialog == DialogAction.SETPRO6) return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
            return false;
        }
        if (targetId == ShaoranyerkNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
            if (dialog == DialogAction.SETPRO7) return await DefaultCloseDialogAsync(env, conn, 6, 7, ct);
            return false;
        }
        if (targetId == ArnesonerkNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 3739, ct);
            if (dialog == DialogAction.SETPRO8) return await DefaultCloseDialogAsync(env, conn, 7, 7, reward: true, sameNpc: false, ct);
            return false;
        }

        return false;
    }
}
