// Port of Java data/scripts/system/handlers/quest/gelkmaros/_21136InSearchOfAWitness.java (Cheatkiller).
// Talk to 799271, gives item 182207919 on accept; relay at 799413 (SETPRO1 removes the item,
// var 0->1) then 799414 (SETPRO2 forces var->2, reward flip); turn in at 730355.
// Skip vs Java: Java's `qe.registerCanAct(questId, 730355)` (an NPC-interactivity gate keyed on
// quest state) has no equivalent hook in this port's IQuestHandler — the handler's own var checks
// already gate the dialog logic, so this only drops an extra client-side interactivity hint.
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

namespace Quest.Gelkmaros;

public sealed class _21136InSearchOfAWitness : QuestHandlerBase
{
    private const int QuestIdConst = 21136;
    private const int StartNpc     = 799271;
    private const int RelayNpc1    = 799413;
    private const int RelayNpc2    = 799414;
    private const int FinalNpc     = 730355;
    private const int ItemId       = 182207919;

    private readonly IItemDao _itemDao;

    public _21136InSearchOfAWitness(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(RelayNpc1).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RelayNpc2).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FinalNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
                {
                    if (await GiveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct))
                        return await SendQuestStartDialogAsync(env, conn, ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == RelayNpc1)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                return false;
            }

            if (targetId == RelayNpc2)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    entry.SetVar(0, 2);
                    return await DefaultCloseDialogAsync(env, conn, 2, 2, reward: true, sameNpc: false, ct);
                }
                return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == FinalNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
