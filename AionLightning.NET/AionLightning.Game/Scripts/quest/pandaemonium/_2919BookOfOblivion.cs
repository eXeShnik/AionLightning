// Port of Java data/scripts/system/handlers/quest/pandaemonium/_2919BookOfOblivion.java.
// Accept default at 204206; chain 204215 (var0 0->1) -> 204192 (var0 1->2) -> object 700212
// (var0 2->3, then later at var0==6 gives item 182207013 and advances to 7) -> back at 204206
// (var0 3->4, SELECT_QUEST_REWARD completes in place once var0==7) -> 204224 hands in gathered
// quest_data.xml collect-items (var0 4->6, no reward) -> back to 700212 for the final USE_OBJECT
// step. Turn in at 204206.
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

public sealed class _2919BookOfOblivion : QuestHandlerBase
{
    private const int QuestIdConst = 2919;
    private const int StartNpc = 204206;
    private const int StepOneNpc = 204215;
    private const int StepTwoNpc = 204192;
    private const int ObjNpc = 700212;
    private const int CollectNpc = 204224;
    private const int PageItem = 182207013;

    private readonly IItemDao _itemDao;

    public _2919BookOfOblivion(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StepOneNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StepTwoNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ObjNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(CollectNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId == StepOneNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 0 && await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == StepTwoNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 1 && await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == ObjNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                {
                    if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    if (var == 6) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO4)
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                if (dialog == DialogAction.SETPRO7)
                {
                    await GiveQuestItemAsync(env.Player, conn, _itemDao, PageItem, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 6, 7, ct);
                }
                return false;
            }
            if (targetId == StartNpc)
            {
                if (var == 7 && dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 3 && await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO5)
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    await RemoveQuestItemAsync(env.Player, conn, _itemDao, PageItem, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 7, 7, reward: true, sameNpc: true, ct);
                }
                return false;
            }
            if (targetId == CollectNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 4 && await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 4, 6, reward: false, checkOkId: 2802, checkFailId: 2717, ct);
                if (dialog == DialogAction.SETPRO6)
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
