// Port of Java data/scripts/system/handlers/quest/inggison/_11110KillingTime.java.
// Talk to Suleion (799075) to start (no item); kill 217039/217040 nine times combined (var 0->9,
// reward on the 9th); turn in at 799075.
// Skip vs Java: qs.canRepeat() (daily-repeat/cooldown) isn't ported (no repeat-tracking infra yet,
// same simplification already documented in other zones, e.g. Scripts/quest/heiron/_18600...cs) —
// the quest still completes once per character exactly like a normal quest.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Inggison;

public sealed class _11110KillingTime : QuestHandlerBase
{
    private const int QuestIdConst = 11110;
    private const int TurnInNpc    = 799075;
    private static readonly int[] Mobs = { 217039, 217040 };

    public _11110KillingTime(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(Mobs[0]).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Mobs[1]).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == TurnInNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var is >= 0 and < 9)
            return await DefaultOnKillEventAsync(env, conn, (IReadOnlyCollection<int>)Mobs, 0, 9, ct);
        if (var == 9)
            return await DefaultOnKillEventAsync(env, conn, (IReadOnlyCollection<int>)Mobs, 9, reward: true, ct);
        return false;
    }
}
