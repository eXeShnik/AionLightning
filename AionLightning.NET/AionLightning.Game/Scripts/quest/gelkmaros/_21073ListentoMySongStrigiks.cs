// Port of Java data/scripts/system/handlers/quest/gelkmaros/_21073ListentoMySongStrigiks.java (HellBoy, reworked vlog).
// Talk to Skilving (799407) to start; relay at Svasuth (799408, var 0->1); back to Skilving
// (var 1, SELECT_QUEST_REWARD flips to REWARD and shows page 5); turn in at Skilving.
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

namespace Quest.Gelkmaros;

public sealed class _21073ListentoMySongStrigiks : QuestHandlerBase
{
    private const int QuestIdConst = 21073;
    private const int SkilvingNpc  = 799407;
    private const int SvasuthNpc   = 799408;

    public _21073ListentoMySongStrigiks(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(SkilvingNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(SkilvingNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SvasuthNpc).OnTalk.Add(QuestId);
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
            if (targetId == SkilvingNpc)
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

            if (targetId == SvasuthNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }

            if (targetId == SkilvingNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    if (var == 1)
                        await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == SkilvingNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
