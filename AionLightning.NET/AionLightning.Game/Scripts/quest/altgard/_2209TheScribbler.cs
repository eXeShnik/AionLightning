// Port of Java data/scripts/system/handlers/quest/altgard/_2209TheScribbler.java (Mr. Poke).
// Talk to 203555, relay through 203562, 203572, 203592, turn in at 203555.
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

namespace Quest.Altgard;

public sealed class _2209TheScribbler : QuestHandlerBase
{
    private const int QuestIdConst = 2209;
    private const int StartNpc     = 203555;
    private const int FirstRelay   = 203562;
    private const int SecondRelay  = 203572;
    private const int ThirdRelay   = 203592;

    public _2209TheScribbler(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FirstRelay).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SecondRelay).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ThirdRelay).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
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
            switch (targetId)
            {
                case FirstRelay when var == 0:
                    if (dialog == DialogAction.QUEST_SELECT)
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (dialog == DialogAction.SETPRO1)
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                    return false;
                case SecondRelay when var == 1:
                    if (dialog == DialogAction.QUEST_SELECT)
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (dialog == DialogAction.SETPRO2)
                        return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                    return false;
                case ThirdRelay when var == 2:
                    if (dialog == DialogAction.QUEST_SELECT)
                        return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    if (dialog == DialogAction.SETPRO3)
                        return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                    return false;
                case StartNpc when var == 3:
                    if (dialog == DialogAction.QUEST_SELECT)
                        return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                    {
                        entry.Status = QuestStatus.REWARD;
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await SendQuestEndDialogAsync(env, conn, ct);
                    }
                    return await SendQuestEndDialogAsync(env, conn, ct);
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
