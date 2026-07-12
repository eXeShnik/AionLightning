// Port of Java data/scripts/system/handlers/quest/sanctum/_1918AnAxForNamus.java (Cheatkiller).
// Talk to Vridian (203835) to start; deliver the axe to 702Berun (203788), which gives quest item
// 182206002 before flipping to REWARD. Java bug fix: the original ignores giveQuestItem's return
// value and advances the quest even when the bag is full; this port bails without advancing when
// the give fails, matching how other fleet scripts already guard this call.
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

namespace Quest.Sanctum;

public sealed class _1918AnAxForNamus : QuestHandlerBase
{
    private const int QuestIdConst = 1918;
    private const int StartNpc     = 203835;
    private const int DeliverNpc   = 203788;
    private const int AxeItemId    = 182206002;

    private readonly IItemDao _itemDao;

    public _1918AnAxForNamus(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(DeliverNpc).OnTalk.Add(QuestId);
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
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == DeliverNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                if (!await GiveQuestItemAsync(player, conn, _itemDao, AxeItemId, 1, ct)) return true;
                entry.SetVar(0, 1);
                return await DefaultCloseDialogAsync(env, conn, 1, 1, reward: true, sameNpc: false, ct);
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, AxeItemId, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
