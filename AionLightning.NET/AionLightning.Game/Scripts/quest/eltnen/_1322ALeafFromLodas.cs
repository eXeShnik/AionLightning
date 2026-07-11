// Port of Java data/scripts/system/handlers/quest/eltnen/_1322ALeafFromLodas.java (MrPoke, remod).
// Talk to Lodas (730019) to start and later to finish; Daminu (730008) flips it to REWARD.
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

namespace Quest.Eltnen;

public sealed class _1322ALeafFromLodas : QuestHandlerBase
{
    private const int QuestIdConst = 1322;
    private const int LodasNpc     = 730019;
    private const int DaminuNpc    = 730008;

    public _1322ALeafFromLodas(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(LodasNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(LodasNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(DaminuNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        var entry = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == LodasNpc)
        {
            if (entry is null || entry.Status == QuestStatus.NONE)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.START)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                {
                    entry.SetVar(0, 2);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestEndDialogAsync(env, conn, ct);
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.REWARD)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        else if (targetId == DaminuNpc)
        {
            if (entry is { Status: QuestStatus.START } && entry.GetVar(0) == 0)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog is DialogAction.SETPRO1 or DialogAction.SETPRO2)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }
        return false;
    }
}
