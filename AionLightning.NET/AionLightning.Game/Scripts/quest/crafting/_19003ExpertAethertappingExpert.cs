// Port of Java data/scripts/system/handlers/quest/crafting/_19003ExpertAethertappingExpert.java (Gigi).
// Doban (203782) hands over the aether tool (182206128) on dialog open; Vateros (798600)
// completes the quest and teaches skill 30003 level 400, granted+persisted via
// QuestHandlerBase.GrantQuestSkillAsync (S2 skill-learning hook, backed by SkillLearnService).
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

namespace Quest.Crafting;

public sealed class _19003ExpertAethertappingExpert : QuestHandlerBase
{
    private const int QuestIdConst = 19003;
    private const int DobanNpc   = 203782;
    private const int VaterosNpc = 798600;
    private const int ToolItemId = 182206128;

    private readonly IItemDao _itemDao;

    public _19003ExpertAethertappingExpert(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        var doban = engine.RegisterQuestNpc(DobanNpc);
        doban.OnQuestStart.Add(QuestId);
        doban.OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(VaterosNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        var status = entry?.Status ?? QuestStatus.NONE;
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (status == QuestStatus.NONE)
        {
            if (targetId != DobanNpc) return false;
            if (dialog != DialogAction.QUEST_SELECT) return await SendQuestStartDialogAsync(env, conn, ct);

            if (!await GiveQuestItemAsync(player, conn, _itemDao, ToolItemId, 1, ct)) return true;
            return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
        }

        if (status == QuestStatus.START)
        {
            if (targetId != VaterosNpc || dialog != DialogAction.QUEST_SELECT) return false;
            await ChangeQuestStepAsync(conn, entry!, -1, 0, toReward: true, ct);
            return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
        }

        if (status == QuestStatus.REWARD)
        {
            if (targetId != VaterosNpc) return false;
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);

            await GrantQuestSkillAsync(player, conn, 30003, 400, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, ToolItemId, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
