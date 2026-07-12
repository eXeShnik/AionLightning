// Port of Java data/scripts/system/handlers/quest/pandaemonium/_4906TalesOfHeroes.java.
// Accept at 204211 gives 182207075; 3-step item-hop chain (each step removes the previous item and
// gives the next one): 205188 var0 0->1 (075->076), 278003 var0 1->2 (076->077), 204057 SETPRO3
// var0=3 straight to REWARD (077->078); turn in at 204211 removes 182207078.
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

public sealed class _4906TalesOfHeroes : QuestHandlerBase
{
    private const int QuestIdConst = 4906;
    private const int StartNpc = 204211;
    private const int StepOneNpc = 205188;
    private const int StepTwoNpc = 278003;
    private const int StepThreeNpc = 204057;

    private const int ItemA = 182207075;
    private const int ItemB = 182207076;
    private const int ItemC = 182207077;
    private const int ItemD = 182207078;

    private readonly IItemDao _itemDao;

    public _4906TalesOfHeroes(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
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
        engine.RegisterQuestNpc(StepThreeNpc).OnTalk.Add(QuestId);
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
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (!await GiveQuestItemAsync(player, conn, _itemDao, ItemA, 1, ct)) return false;
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == StepOneNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return entry.GetVar(0) == 0 && await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, ItemA, 1, ct);
                    await GiveQuestItemAsync(player, conn, _itemDao, ItemB, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                return false;
            }
            if (targetId == StepTwoNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return entry.GetVar(0) == 1 && await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, ItemB, 1, ct);
                    await GiveQuestItemAsync(player, conn, _itemDao, ItemC, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                }
                return false;
            }
            if (targetId == StepThreeNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return entry.GetVar(0) == 2 && await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO3)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, ItemC, 1, ct);
                    await GiveQuestItemAsync(player, conn, _itemDao, ItemD, 1, ct);
                    await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: true, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, ItemD, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
