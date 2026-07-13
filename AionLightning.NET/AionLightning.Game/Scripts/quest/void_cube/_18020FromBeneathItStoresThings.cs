// Port of Java data/scripts/system/handlers/quest/void_cube/_18020FromBeneathItStoresThings.java.
// Level-up auto-offered quest at Atika (800570): accept, then SELECT_QUEST_REWARD in START flips
// straight to REWARD and hands in. Turn in at Atika.
// note: SendQuestStartDialogAsync in this port only handles QUEST_ACCEPT_1/QUEST_REFUSE*, not the
// QUEST_ACCEPT_SIMPLE simplified-accept leg, so that leg drives StartMissionAsync + close directly
// (matches weapon_enchant/_22409ThePerfectGun precedent).
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

public sealed class _18020FromBeneathItStoresThings : QuestHandlerBase
{
    private const int QuestIdConst = 18020;
    private const int AtikaNpc     = 800570;

    public _18020FromBeneathItStoresThings(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(AtikaNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(AtikaNpc).OnTalk.Add(QuestId);
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
            if (targetId == AtikaNpc)
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
            if (targetId == AtikaNpc)
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

        if (entry.Status == QuestStatus.REWARD && targetId == AtikaNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
