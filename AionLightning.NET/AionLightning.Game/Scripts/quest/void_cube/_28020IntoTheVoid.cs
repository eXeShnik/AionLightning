// Port of Java data/scripts/system/handlers/quest/void_cube/_28020IntoTheVoid.java.
// Asmodian counterpart of _18020FromBeneathItStoresThings — identical structure at Vesmo (800572):
// level-up auto-offered, accept, SELECT_QUEST_REWARD in START flips straight to REWARD, turn in.
// note: QUEST_ACCEPT_SIMPLE drives StartMissionAsync + close directly (SendQuestStartDialogAsync
// doesn't cover the simplified-accept leg in this port).
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

namespace Quest.VoidCube;

public sealed class _28020IntoTheVoid : QuestHandlerBase
{
    private const int QuestIdConst = 28020;
    private const int VesmoNpc     = 800572;

    public _28020IntoTheVoid(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(VesmoNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(VesmoNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == VesmoNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.QUEST_ACCEPT_1)
                    return await SendQuestStartDialogAsync(env, conn, ct);
                if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
                {
                    await StartMissionAsync(conn, player, QuestStatus.START, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == VesmoNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                    return await SendQuestEndDialogAsync(env, conn, ct);
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == VesmoNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
