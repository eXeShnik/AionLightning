// Port of Java data/scripts/system/handlers/quest/argent_manor/_30461RingAroundtheShugo.java (Ritsu).
// Accept at 204108 (QUEST_ACCEPT_SIMPLE gives item 182213032, then starts); report to 799547
// (SETPRO1 removes the item and flips to REWARD); turn in at 799546.
// The QUEST_ACCEPT_SIMPLE accept leg gives the item itself and drives StartMissionAsync +
// CloseDialogWindowAsync directly (matches quest/sarpan/_11524FieldRepairs.cs precedent) since
// SendQuestStartDialogAsync in this port only handles QUEST_ACCEPT/QUEST_ACCEPT_1/QUEST_REFUSE*,
// not QUEST_ACCEPT_SIMPLE (Java's fuller sendQuestStartDialog switch isn't ported 1:1).
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

namespace Quest.ArgentManor;

public sealed class _30461RingAroundtheShugo : QuestHandlerBase
{
    private const int QuestIdConst = 30461;
    private const int StartNpc     = 204108;
    private const int RewardNpc    = 799546;
    private const int ReportNpc    = 799547;
    private const int TokenItem    = 182213032;

    private readonly IItemDao _itemDao;

    public _30461RingAroundtheShugo(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RewardNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ReportNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;

            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);

            if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
            {
                if (!await GiveQuestItemAsync(player, conn, _itemDao, TokenItem, 1, ct)) return true;
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }

            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId != ReportNpc) return false;
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 0:
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                case DialogAction.SETPRO1:
                    await RemoveQuestItemAsync(player, conn, _itemDao, TokenItem, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 0, 0, reward: true, sameNpc: false, ct);
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != RewardNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
