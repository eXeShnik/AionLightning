// Port of Java data/scripts/system/handlers/quest/theobomos/_3031Pirates.java ("Wanted: Pirates").
// Accept at the wanted board (730144); kill 15 of either 214219/214220 (var 1) and 12 of either
// 214222/214223 (var 2) to flip to REWARD; turn in at Erinyes (798172).
using System;
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Theobomos;

public sealed class _3031Pirates : QuestHandlerBase
{
    private const int QuestIdConst = 3031;
    private const int BoardObj     = 730144;
    private const int ErinyesNpc   = 798172;
    private static readonly int[] Group1 = [214219, 214220];
    private static readonly int[] Group2 = [214222, 214223];

    public _3031Pirates(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(BoardObj).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(BoardObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ErinyesNpc).OnTalk.Add(QuestId);
        foreach (int npc in Group1) engine.RegisterQuestNpc(npc).OnKill.Add(QuestId);
        foreach (int npc in Group2) engine.RegisterQuestNpc(npc).OnKill.Add(QuestId);
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
            if (targetId != BoardObj) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(0, 0), ct);
                return true;
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD && targetId == ErinyesNpc)
        {
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int targetId = env.TargetId;
        if (Array.IndexOf(Group1, targetId) >= 0 && entry.GetVar(1) < 15)
        {
            entry.SetVar(1, entry.GetVar(1) + 1);
            await UpdateQuestStatusAsync(conn, entry, ct);
            if (entry.GetVar(1) == 15 && entry.GetVar(2) == 12)
            {
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
            }
            return true;
        }
        if (Array.IndexOf(Group2, targetId) >= 0 && entry.GetVar(2) < 12)
        {
            entry.SetVar(2, entry.GetVar(2) + 1);
            await UpdateQuestStatusAsync(conn, entry, ct);
            if (entry.GetVar(1) == 15 && entry.GetVar(2) == 12)
            {
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
            }
            return true;
        }
        return false;
    }
}
