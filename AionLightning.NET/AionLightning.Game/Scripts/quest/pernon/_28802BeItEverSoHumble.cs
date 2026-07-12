// Port of Java data/scripts/system/handlers/quest/pernon/_28802BeItEverSoHumble.java (zhkchi).
// Talk to 830102 to start; talk to 830153 in START status (var 0->0, reward) then again in REWARD
// status to turn in. Skip vs Java: the Ulaguru-style HousingService.registerPlayerStudio(player)
// side effect fired on SELECTED_QUEST_NOREWARD isn't ported (no HousingService/House model exists
// in this port at all — see migration_plan.md housing gap) — it's a one-off side effect, not a
// progression gate, so the quest still starts/advances/completes end-to-end without it.
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

namespace Quest.Pernon;

public sealed class _28802BeItEverSoHumble : QuestHandlerBase
{
    private const int QuestIdConst = 28802;
    private const int StartNpc     = 830102;
    private const int TurnInNpc    = 830153;

    public _28802BeItEverSoHumble(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
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
                if (dialog is DialogAction.QUEST_ACCEPT_1 or DialogAction.QUEST_ACCEPT_SIMPLE)
                    return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            // Java: SELECTED_QUEST_NOREWARD also called HousingService.registerPlayerStudio(player)
            // here (documented no-op skip above).
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
