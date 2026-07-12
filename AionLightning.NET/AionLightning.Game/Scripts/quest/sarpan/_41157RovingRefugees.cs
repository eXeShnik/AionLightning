// Port of Java data/scripts/system/handlers/quest/sarpan/_41157RovingRefugees.java (zhkchi).
// Talk to Kayron (205583) to start (QUEST_ACCEPT_SIMPLE, no item); 3-stop fetch chain at
// 205990 (var 0->1), 205991 (var 1->2), 205992 (var 2->3); back at Kayron (var 3,
// SELECT_QUEST_REWARD flips to REWARD, page 5, then finish).
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

public sealed class _41157RovingRefugees : QuestHandlerBase
{
    private const int QuestIdConst = 41157;
    private const int KayronNpc     = 205583;
    private const int Npc205990     = 205990;
    private const int Npc205991     = 205991;
    private const int Npc205992     = 205992;

    public _41157RovingRefugees(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(KayronNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(KayronNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc205990).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc205991).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc205992).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != KayronNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == Npc205990)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == Npc205991)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == Npc205992)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO3) return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                return false;
            }
            if (targetId == KayronNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD && var == 3)
                {
                    await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == KayronNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
