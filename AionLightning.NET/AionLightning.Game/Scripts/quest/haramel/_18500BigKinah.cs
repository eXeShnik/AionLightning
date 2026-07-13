// Port of Java data/scripts/system/handlers/quest/haramel/_18500BigKinah.java.
// Odium-sample fetch quest: talk to Alisdair (203106) to start; hand off to Zephyros (203166),
// then use the Suspicious Odium Piece (730304) then Pile (730305) objects to advance var 0..3;
// entering the Pandaemonium map (300200000) while at var 3 auto-flips the quest to REWARD; turn
// in at Moorilerk (799522). NPC 206150 is registered exactly like Java but never referenced in
// onDialogEvent (kept as a no-op OnTalk registration for 1:1 parity).
using System.Linq;
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

namespace Quest.Haramel;

public sealed class _18500BigKinah : QuestHandlerBase
{
    private const int QuestIdConst        = 18500;
    private const int AlisdairNpc         = 203106;
    private const int ZephyrosNpc         = 203166;
    private const int OdiumPieceObj       = 730304;
    private const int OdiumPileObj        = 730305;
    private const int MoorilerkNpc        = 799522;
    private const int UnusedNpc           = 206150;
    private const int PandaemoniumWorldId = 300200000;

    public _18500BigKinah(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterQuestNpc(AlisdairNpc).OnQuestStart.Add(QuestId);
        foreach (int npc in new[] { AlisdairNpc, ZephyrosNpc, OdiumPieceObj, OdiumPileObj, MoorilerkNpc, UnusedNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is not null && entry.Status == QuestStatus.START
            && player.Position.WorldId == PandaemoniumWorldId && entry.GetVar(0) == 3)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: true, ct);
            return true;
        }
        return false;
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
            if (targetId == AlisdairNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId == ZephyrosNpc)
            {
                // Java switch fallthrough: QUEST_SELECT with var != 0 falls into SETPRO1's defaultCloseDialog.
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }

            if (targetId == OdiumPieceObj)
            {
                // Java switch fallthrough: USE_OBJECT with var != 1 falls into SETPRO2's defaultCloseDialog.
                if (dialog == DialogAction.USE_OBJECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.USE_OBJECT || dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }

            if (targetId == OdiumPileObj)
            {
                // Java switch fallthrough: USE_OBJECT with var != 2 falls into SETPRO3's defaultCloseDialog.
                if (dialog == DialogAction.USE_OBJECT && var == 2)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.USE_OBJECT || dialog == DialogAction.SETPRO3)
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == MoorilerkNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
