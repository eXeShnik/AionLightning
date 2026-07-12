// Port of Java data/scripts/system/handlers/quest/inggison/_11294SpawningInvestigation.java.
// Talk to 799092 to start — Java's sendQuestStartDialog(env, itemId, itemCount) overload (give the
// item as part of accept) is inlined here since only this quest in the batch needs it: accepting
// starts the quest, gives 182213038 and shows page 1003. Relay at 799010 (var 0) removes the item
// and self-sets var 2 before flipping to reward, mirroring Java's redundant SETPRO1 gate (same
// idiom as _11212BalaurRecords.cs). Turn in at 798926.
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

namespace Quest.Inggison;

public sealed class _11294SpawningInvestigation : QuestHandlerBase
{
    private const int QuestIdConst = 11294;
    private const int StartNpc     = 799092;
    private const int RelayNpc     = 799010;
    private const int TurnInNpc    = 798926;
    private const int ClueItem     = 182213038;

    private readonly IItemDao _itemDao;

    public _11294SpawningInvestigation(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(RelayNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                if (!await StartMissionAsync(conn, player, QuestStatus.START, ct)) return false;
                await GiveQuestItemAsync(player, conn, _itemDao, ClueItem, 1, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == RelayNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, ClueItem, 1, ct);
                entry.SetVar(0, 2);
                return await DefaultCloseDialogAsync(env, conn, 2, 2, reward: true, sameNpc: false, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
