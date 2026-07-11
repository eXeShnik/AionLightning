// Port of Java data/scripts/system/handlers/quest/eltnen/_1311AGermOfHope.java (MrPoke, remod).
// Start at 203997 (gives a germ sample 182201305 via SELECT_ACTION_1013); use the Tombstone
// (700164) to consume it -> REWARD; return to 203997 to finish (removes the sample again as a
// belt-and-suspenders cleanup, matching Java's redundant removeQuestItem in the CHECK branch).
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

namespace Quest.Eltnen;

public sealed class _1311AGermOfHope : QuestHandlerBase
{
    private const int QuestIdConst  = 1311;
    private const int SamplerNpc    = 203997;
    private const int TombstoneObj  = 700164;
    private const int SampleItemId  = 182201305;

    private readonly IItemDao _itemDao;

    public _1311AGermOfHope(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(SamplerNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(SamplerNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TombstoneObj).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != SamplerNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (env.DialogId == (int)DialogAction.SELECT_ACTION_1013)
            {
                if (!await GiveQuestItemAsync(player, conn, _itemDao, SampleItemId, 1, ct)) return true;
                return await SendQuestDialogAsync(conn, targetObjId, 4, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == TombstoneObj)
            {
                if (entry.GetVar(0) == 0 && dialog == DialogAction.USE_OBJECT)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, SampleItemId, 1, ct);
                    entry.SetVar(0, 1);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return true;
                }
                return false;
            }
            if (targetId == SamplerNpc && entry.GetVar(0) == 1)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, SampleItemId, 1, ct);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == SamplerNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
