// Port of Java data/scripts/system/handlers/quest/hero/_23530AJustReward.java (Elyos mirror of
// _13530AHeroReward). Auto-starts on level-up; talk to the quest npc to finish immediately.
// NOTE: the Java source registers/uses npc 800527 verbatim here — the same id as Tirins, the
// Asmodian hub in _13530AHeroReward — rather than an Elyos-side npc (e.g. 800529, used by every
// other Elyos hero quest in this folder). This looks like a copy-paste artifact in the original
// data, but it isn't a logic bug (no broken control flow), so the id is ported unchanged.
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

namespace Quest.Hero;

public sealed class _23530AJustReward : QuestHandlerBase
{
    private const int QuestIdConst = 23530;
    private const int QuestHubNpc  = 800527; // Java source uses this id verbatim; see header note.

    public _23530AJustReward(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(QuestHubNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(QuestHubNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId != QuestHubNpc) return false;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog is DialogAction.QUEST_ACCEPT_1 or DialogAction.QUEST_ACCEPT_SIMPLE)
                return await SendQuestStartDialogAsync(env, conn, ct);
            return false;
        }

        if (entry.Status == QuestStatus.START)
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

        if (entry.Status == QuestStatus.REWARD)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
