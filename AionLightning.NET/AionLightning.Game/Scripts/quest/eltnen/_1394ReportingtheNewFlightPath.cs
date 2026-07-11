// Port of Java data/scripts/system/handlers/quest/eltnen/_1394ReportingtheNewFlightPath.java (Altaress).
// Talk to 204041 to start; advance at 204039 (var 0->1); report at 203946 (var forced to 2 ->
// REWARD via the shared close-dialog helper, matching Java's setQuestVar(2) + defaultCloseDialog);
// finish at 203941.
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

public sealed class _1394ReportingtheNewFlightPath : QuestHandlerBase
{
    private const int QuestIdConst = 1394;
    private const int StartNpc     = 204041;
    private const int SecondNpc    = 204039;
    private const int ThirdNpc     = 203946;
    private const int FourthNpc    = 203941;

    public _1394ReportingtheNewFlightPath(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SecondNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ThirdNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FourthNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, env.Target?.ObjectId ?? 0, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == SecondNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 0)
                    return await SendQuestDialogAsync(conn, env.Target?.ObjectId ?? 0, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
            else if (targetId == ThirdNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 1)
                    return await SendQuestDialogAsync(conn, env.Target?.ObjectId ?? 0, 1693, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    entry.SetVar(0, 2);
                    return await DefaultCloseDialogAsync(env, conn, 2, 2, reward: true, sameNpc: false, ct);
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == FourthNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, env.Target?.ObjectId ?? 0, 2375, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
