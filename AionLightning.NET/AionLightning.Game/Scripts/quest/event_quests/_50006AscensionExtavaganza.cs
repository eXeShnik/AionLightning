// Port of Java data/scripts/system/handlers/quest/event_quests/_50006AscensionExtavaganza.java.
// Identical structure to _50005DaevasDayEnergy (same npc, Gracia 799933); only the quest_data.xml
// reward differs. See _50005 for the targetId==0 accept idiom and the EventService skip note.
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

namespace Quest.EventQuests;

public sealed class _50006AscensionExtavaganza : QuestHandlerBase
{
    private const int QuestIdConst = 50006;
    private const int GraciaNpc    = 799933;

    public _50006AscensionExtavaganza(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(GraciaNpc).OnTalk.Add(QuestId);
        engine.RegisterOnLevelUp(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (env.TargetId == 0)
        {
            if (dialog == DialogAction.QUEST_ACCEPT_1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await CloseDialogWindowAsync(conn, 0, ct);
            }
            return false;
        }

        if (env.TargetId != GraciaNpc) return false;

        var entry = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetObjId = env.Target?.ObjectId ?? 0;
        if (dialog == DialogAction.QUEST_SELECT && entry.Status == QuestStatus.START)
            return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);

        if (dialog == DialogAction.SELECT_QUEST_REWARD)
            await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: true, ct);

        return await SendQuestEndDialogAsync(env, conn, ct);
    }
}
