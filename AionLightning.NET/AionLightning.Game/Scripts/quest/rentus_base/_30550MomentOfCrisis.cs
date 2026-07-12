// Port of Java data/scripts/system/handlers/quest/rentus_base/_30550MomentOfCrisis.java (Ritsu).
// Start at Skafir (205864); report to Maios (799549, var0->1); once var0==1, tell Oreitia
// (799544) to flip straight to REWARD; turn in at Oreitia.
//
// Java bug fixed: the outer `switch (targetId) { case 799549: {...} case 799544: {...} }` has no
// break after the 799549 block, so any dialog id unmatched there (i.e. anything but QUEST_SELECT/
// SETPRO1) falls through into 799544's switch on that same dialog value. In practice the client
// never sends 799544-only dialog ids (SELECT_QUEST_REWARD) while targeting 799549, so this is dead
// weight rather than a live bug — still fixed by checking each npc branch independently, matching
// the convention used elsewhere in this batch (see ascension._1913DispatchtoVerteron).
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

namespace Quest.RentusBase;

public sealed class _30550MomentOfCrisis : QuestHandlerBase
{
    private const int QuestIdConst = 30550;
    private const int StartNpc     = 205864; // Skafir
    private const int MaiosNpc     = 799549;
    private const int OreitiaNpc   = 799544;

    public _30550MomentOfCrisis(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MaiosNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(OreitiaNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId == MaiosNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }

            if (targetId == OreitiaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD && var == 1)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return false;
            }

            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == OreitiaNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
