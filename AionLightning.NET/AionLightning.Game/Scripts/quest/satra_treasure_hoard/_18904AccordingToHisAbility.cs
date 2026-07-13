// Port of Java data/scripts/system/handlers/quest/satra_treasure_hoard/_18904AccordingToHisAbility.java
// (Ritsu). Apsilon (800331) is registered for both OnQuestStart and OnTalk (re-shows the start
// dialog even mid-quest, matching Java's identical branch for NONE and START statuses at that npc).
// Collector Karuti (205844) flips to REWARD once var 0 reaches 9 (9 registered mob ids, kill-once
// span 0..9 via DefaultOnKillEventAsync). Java's redundant `env.getVisibleObject() instanceof Npc`
// target re-resolution in onDialogEvent is dropped — env.TargetId already reflects the dialog
// target npc in this port, matching how other already-ported scripts (e.g. reshanta) never carry
// that re-cast either.
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

namespace Quest.SatraTreasureHoard;

public sealed class _18904AccordingToHisAbility : QuestHandlerBase
{
    private const int QuestIdConst = 18904;
    private const int ApsilonNpc   = 800331;
    private const int KarutiNpc    = 205844;

    private static readonly int[] _killNpcIds =
        [219302, 219303, 219304, 219305, 219306, 219307, 219308, 219309, 219310];

    public _18904AccordingToHisAbility(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(ApsilonNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(ApsilonNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KarutiNpc).OnTalk.Add(QuestId);
        foreach (int npcId in _killNpcIds)
            engine.RegisterQuestNpc(npcId).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, _killNpcIds, 0, 9, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != ApsilonNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == ApsilonNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (targetId == KarutiNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (entry.GetVar(0) == 9)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != KarutiNpc) return false;
            return dialog switch
            {
                DialogAction.USE_OBJECT          => await SendQuestDialogAsync(conn, targetObjId, 1008, ct),
                DialogAction.SELECT_QUEST_REWARD => await SendQuestDialogAsync(conn, targetObjId, 5, ct),
                _                                 => await SendQuestEndDialogAsync(env, conn, ct),
            };
        }

        return false;
    }
}
