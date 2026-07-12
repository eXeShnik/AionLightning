// Port of Java data/scripts/system/handlers/quest/heiron/_1604ToCatchASpy.java.
// Talk to 204576 to start; attacking the spy (212615) within 8m of a fixed spot finishes it off
// and flips straight to REWARD; turn in at 204576.
// Skip vs Java: the explicit kill-the-target call (Npc.getController().onDie) isn't ported — no
// NPC-death-trigger controller infra exists yet; the quest still completes on the qualifying attack.
using System;
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

namespace Quest.Heiron;

public sealed class _1604ToCatchASpy : QuestHandlerBase
{
    private const int QuestIdConst = 1604;
    private const int StartNpc = 204576;
    private const int SpyNpc   = 212615;
    private const float SpotX = 717.78f;
    private const float SpotY = 623.50f;
    private const float SpotZ = 130f;
    private const float RequiredDistance = 8f;

    public _1604ToCatchASpy(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SpyNpc).OnAttack.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var entry  = player.Quests.Get(QuestId);
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnAttackAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 0) return false;
        if (env.TargetId != SpyNpc || env.Target is null) return false;

        float dx = env.Target.Position.X - SpotX;
        float dy = env.Target.Position.Y - SpotY;
        float dz = env.Target.Position.Z - SpotZ;
        if (MathF.Sqrt(dx * dx + dy * dy + dz * dz) >= RequiredDistance) return false;

        entry.SetVar(0, 1);
        entry.Status = QuestStatus.REWARD;
        await UpdateQuestStatusAsync(conn, entry, ct);
        return false;
    }
}
