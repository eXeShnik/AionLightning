// Port of Java data/scripts/system/handlers/quest/pandaemonium/_2925AHeartfeltConfession.java.
// Accept default at 204261; 204235 gates its first dialog on whether item 110100288 is currently
// equipped (1011 if so, else 1097); 4-step chain (204235 var0 0->1, 204261 var0 1->2 removing the
// item, 204127 var0 2->3, 204193 var0 3->4, 204235 var0 4->5); the SELECT_ACTION_2717 branch at
// 204261 writes var *slot 5* (not slot 0) while flipping to REWARD — a Java quirk ported as-is
// since it's inert either way (the var0==5 dialog-2716 check it was seemingly meant to satisfy is
// unreachable once status is REWARD, matching original Java behavior exactly); turn in at 204261.
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

namespace Quest.Pandaemonium;

public sealed class _2925AHeartfeltConfession : QuestHandlerBase
{
    private const int QuestIdConst = 2925;
    private const int StartNpc = 204261;
    private const int StepOneNpc = 204235;
    private const int StepThreeNpc = 204127;
    private const int StepFourNpc = 204193;
    private const int RingItem = 110100288;

    private readonly IItemDao _itemDao;

    public _2925AHeartfeltConfession(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StepOneNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StepThreeNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StepFourNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId == StepOneNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0)
                    {
                        bool ringEquipped = player.Inventory.FindByItemId(RingItem, includeEquipped: true) is { IsEquipped: true };
                        return await SendQuestDialogAsync(conn, targetObjId, ringEquipped ? 1011 : 1097, ct);
                    }
                    if (var == 4)
                        return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                if (dialog == DialogAction.SETPRO5)
                    return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                return false;
            }
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 5) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO2)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, RingItem, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                }
                if (dialog == DialogAction.SELECT_ACTION_2717)
                {
                    await ChangeQuestStepAsync(conn, entry, 5, 5, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                }
                return false;
            }
            if (targetId == StepThreeNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 2 && await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3)
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                return false;
            }
            if (targetId == StepFourNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 3 && await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO4)
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
