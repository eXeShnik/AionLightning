// Port of Java data/scripts/system/handlers/quest/morheim/_4732GeneralMalevolence.java.
// Kelmar (800519) grants a quest item on accept (Java sendQuestStartDialog(env, itemId, count),
// inlined here since QuestHandlerBase only ports the no-item overload); kill 256694 once
// (var 0->1), then 256693 once more to flip to REWARD; turn in at Kelmar.
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

namespace Quest.Morheim;

public sealed class _4732GeneralMalevolence : QuestHandlerBase
{
    private const int QuestIdConst  = 4732;
    private const int KelmarNpc     = 800519;
    private const int FirstMobNpc   = 256694;
    private const int SecondMobNpc  = 256693;
    private const int StartItemId   = 182205676;

    private readonly IItemDao _itemDao;

    public _4732GeneralMalevolence(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(KelmarNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(KelmarNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FirstMobNpc).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(SecondMobNpc).OnKill.Add(QuestId);
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
            if (targetId != KelmarNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);

            switch (dialog)
            {
                case DialogAction.QUEST_ACCEPT or DialogAction.QUEST_ACCEPT_1:
                    if (!await StartMissionAsync(conn, player, QuestStatus.START, ct)) return false;
                    await GiveQuestItemAsync(player, conn, _itemDao, StartItemId, 1, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
                case DialogAction.QUEST_REFUSE or DialogAction.QUEST_REFUSE_1 or DialogAction.QUEST_REFUSE_2 or DialogAction.QUEST_REFUSE_SIMPLE:
                    return await SendQuestDialogAsync(conn, targetObjId, 0, ct);
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == KelmarNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var == 0)
            return await DefaultOnKillEventAsync(env, conn, FirstMobNpc, 0, 1, ct);
        if (var == 1)
            return await DefaultOnKillEventAsync(env, conn, SecondMobNpc, 1, reward: true, ct);
        return false;
    }
}
