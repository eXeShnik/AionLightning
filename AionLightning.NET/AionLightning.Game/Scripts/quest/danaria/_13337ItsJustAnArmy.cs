// Port of Java data/scripts/system/handlers/quest/danaria/_13337ItsJustAnArmy.java (Romanz).
// Accept at 801043 (page 4762 -> ASK_QUEST_ACCEPT page 4 -> QUEST_ACCEPT_1 starts -> QUEST_REFUSE_1
// page 1004); entering the LDF5B sensory area moves var 0 -> 1, then kill six Balaur army mobs
// (231348-231353) to reach var 10, hand in the collected items (CHECK_USER_HAS_QUEST_ITEM) 10 -> 11
// flipping to REWARD; turn in at 801043.
// note: Java also wires the same 0 -> 1 trigger onto npc 206321 via addOnAtDistanceEvent; there is no
// onAtDistance hook in this port, but registerOnEnterZone covers the identical transition, so the
// distance registration is dropped without loss of progression.
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

namespace Quest.Danaria;

public sealed class _13337ItsJustAnArmy : QuestHandlerBase
{
    private const int QuestIdConst = 13337;
    private const int StartNpc     = 801043;
    private const string EnterZoneName = "LDF5B_SENSORYAREA_Q13337_206321_2_600060000";

    private static readonly int[] Mobs = [231348, 231349, 231350, 231351, 231352, 231353];

    private readonly IItemDao _itemDao;

    public _13337ItsJustAnArmy(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, EnterZoneName);
        foreach (int mob in Mobs)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (targetId != StartNpc) return false;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                case DialogAction.ASK_QUEST_ACCEPT:
                    return await SendQuestDialogAsync(conn, targetObjId, 4, ct);
                case DialogAction.QUEST_ACCEPT_1:
                    return await SendQuestStartDialogAsync(env, conn, ct);
                case DialogAction.QUEST_REFUSE_1:
                    return await SendQuestDialogAsync(conn, targetObjId, 1004, ct);
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.START)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 10, 11, reward: true, checkOkId: 10000, checkFailId: 10001, ct);
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, Mobs, 1, 10, ct);

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != EnterZoneName) return false;
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) != 0) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
        return true;
    }
}
