// Port of Java data/scripts/system/handlers/quest/pernon/_28808OpenSaysMe.java (Ritsu).
// Talk to 830392 to start (gives quest item 182213216 on accept, aborting the start if the bag is
// full); use object 730534 (var 0, dialog 2375) to flip to REWARD; turn in at 730534. Java's
// USE_OBJECT case falls into SELECT_QUEST_REWARD when var != 0 — harmless, since changeQuestStep's
// caller here doesn't gate on var, so it's kept 1:1 (see _28805 for the fallthrough that actually
// mattered and was fixed).
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

namespace Quest.Pernon;

public sealed class _28808OpenSaysMe : QuestHandlerBase
{
    private const int QuestIdConst = 28808;
    private const int StartNpc     = 830392;
    private const int ChestNpc     = 730534;
    private const int KeyItemId    = 182213216;

    private readonly IItemDao _itemDao;

    public _28808OpenSaysMe(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ChestNpc).OnTalk.Add(QuestId);
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
                if (dialog is DialogAction.QUEST_ACCEPT_SIMPLE or DialogAction.QUEST_ACCEPT_1)
                {
                    if (await GiveQuestItemAsync(player, conn, _itemDao, KeyItemId, 1, ct))
                        return await SendQuestStartDialogAsync(env, conn, ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == ChestNpc)
        {
            int var = entry.GetVar(0);
            if (dialog == DialogAction.USE_OBJECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog is DialogAction.USE_OBJECT or DialogAction.SELECT_QUEST_REWARD)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == ChestNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
