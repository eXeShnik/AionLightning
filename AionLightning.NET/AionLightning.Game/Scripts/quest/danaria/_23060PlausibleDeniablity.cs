// Port of Java data/scripts/system/handlers/quest/danaria/_23060PlausibleDeniablity.java.
// Talk to Tenitz (801104) to start; Ulvari's (801131) SETPRO1 bumps var 0->1 (still START, raw var
// write matching Java's direct qs.setQuestVarById/updateQuestStatus rather than the shared helper);
// turn in at any of Ubarunerk/Dairunrunerk/Algarunerk (800936/800937/800938), which flips to reward
// (var set to 1, same value it already held - no discrepancy with the shared reward-flip helper).
using System;
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

namespace Quest.Danaria;

public sealed class _23060PlausibleDeniablity : QuestHandlerBase
{
    private const int QuestIdConst = 23060;
    private const int StartNpc     = 801104;
    private const int UlvariNpc    = 801131;
    private static readonly int[] TurnInNpcs = { 800936, 800937, 800938 };

    public _23060PlausibleDeniablity(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(UlvariNpc).OnTalk.Add(QuestId);
        foreach (int npc in TurnInNpcs)
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartNpc && (entry is null || entry.Status == QuestStatus.NONE))
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }
        if (entry is null) return false;

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == UlvariNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    entry.SetVar(0, entry.GetVar(0) + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return false;
            }
            if (Array.IndexOf(TurnInNpcs, targetId) >= 0)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    entry.SetVar(0, 1);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestEndDialogAsync(env, conn, ct);
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && Array.IndexOf(TurnInNpcs, targetId) >= 0)
        {
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
