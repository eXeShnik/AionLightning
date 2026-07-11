// Port of Java data/scripts/system/handlers/quest/altgard/_2252ChasingtheLegend.java (Ritsu).
// Talk to Sinood (203646), use the Bone of Minusha (700060) to spawn Minusha's Spirit (210634),
// kill it, turn in.
// Skip vs Java: the 3s SM_USE_OBJECT/SM_EMOTION loot-animation delay before the spirit spawns is
// omitted — the spirit spawns immediately on using the bone instead of after the animation.
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

public sealed class _2252ChasingtheLegend : QuestHandlerBase
{
    private const int QuestIdConst = 2252;
    private const int SinoodNpc    = 203646;
    private const int BoneObj      = 700060;
    private const int SpiritNpc    = 210634;

    public _2252ChasingtheLegend(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(SinoodNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(SinoodNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BoneObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SpiritNpc).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == SinoodNpc)
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
            if (targetId == SinoodNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                {
                    entry.SetVar(0, 2);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                }
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD && var == 2)
                    return await SendQuestEndDialogAsync(env, conn, ct);
                return false;
            }
            if (targetId == BoneObj && dialog == DialogAction.USE_OBJECT && var == 0 && env.Target is not null)
            {
                var pos = env.Target.Position;
                SpawnQuestNpc(pos.WorldId, pos.InstanceId, SpiritNpc, pos.X, pos.Y, pos.Z, (byte)pos.Heading);
                return true;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.TargetId != SpiritNpc || entry.GetVar(0) != 0) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
        return true;
    }
}
