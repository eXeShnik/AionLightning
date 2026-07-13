// Port of Java data/scripts/system/handlers/quest/satra_treasure_hoard/_28904ApsilonsAbilities.java
// (Ritsu). Elyos counterpart of _18904AccordingToHisAbility, but structured like the 189xx START/
// REWARD collector idiom instead: start at 800331 (OnQuestStart only, no separate OnTalk), collector
// 205866 (OnTalk). Only 8 mob ids are registered (219302-219309, no 219310), yet the kill-count span
// is 0..10 (not 0..8) in both Java's onKillEvent and the completion check at the collector — killing
// any of those 8 npcs up to 10 times total (repeats allowed) satisfies the objective.
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

public sealed class _28904ApsilonsAbilities : QuestHandlerBase
{
    private const int QuestIdConst = 28904;
    private const int StartNpc     = 800331;
    private const int CollectorNpc = 205866;

    private static readonly int[] _killNpcIds =
        [219302, 219303, 219304, 219305, 219306, 219307, 219308, 219309];

    public _28904ApsilonsAbilities(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(CollectorNpc).OnTalk.Add(QuestId);
        foreach (int npcId in _killNpcIds)
            engine.RegisterQuestNpc(npcId).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, _killNpcIds, 0, 10, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId != CollectorNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 10)
            {
                entry.SetVar(0, entry.GetVar(0) + 1);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != CollectorNpc) return false;
            return dialog switch
            {
                DialogAction.QUEST_SELECT        => await SendQuestDialogAsync(conn, targetObjId, 5, ct),
                DialogAction.SELECT_QUEST_REWARD => await SendQuestDialogAsync(conn, targetObjId, 5, ct),
                _                                 => await SendQuestEndDialogAsync(env, conn, ct),
            };
        }

        return false;
    }
}
