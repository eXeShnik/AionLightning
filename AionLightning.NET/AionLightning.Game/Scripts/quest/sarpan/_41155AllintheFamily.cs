// Port of Java data/scripts/system/handlers/quest/sarpan/_41155AllintheFamily.java (zhkchi).
// Talk to Dorochi (205592) to start (QUEST_ACCEPT_SIMPLE, no item); fetch chain at 800082
// (var 0->1) then 800083 (var 1->2); turn in at Vaizel (205572, var 2, SELECT_QUEST_REWARD
// flips to REWARD, page 5, then finish).
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

public sealed class _41155AllintheFamily : QuestHandlerBase
{
    private const int QuestIdConst = 41155;
    private const int DorochiNpc    = 205592;
    private const int Npc800082     = 800082;
    private const int Npc800083     = 800083;
    private const int VaizelNpc     = 205572;

    public _41155AllintheFamily(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(DorochiNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(DorochiNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc800082).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc800083).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(VaizelNpc).OnTalk.Add(QuestId);
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
            if (targetId != DorochiNpc) return false;
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
            if (targetId == Npc800082)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == Npc800083)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == VaizelNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD && var == 2)
                {
                    await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == VaizelNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
