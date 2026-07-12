// Port of Java data/scripts/system/handlers/quest/crafting/_19001ExpertEssencetappingExpert.java (Gigi).
// Cornelius (203780) hands over the tapping tool (182206127) the moment the player opens his
// dialog (even before the quest is accepted — matches Java's giveQuestItem-before-accept order);
// Vateros the essence-tapping trainer (798600) completes the quest and teaches skill 30002
// level 400 (Java addSkill), now granted+persisted via QuestHandlerBase.GrantQuestSkillAsync
// (S2 skill-learning hook, backed by SkillLearnService).
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

public sealed class _19001ExpertEssencetappingExpert : QuestHandlerBase
{
    private const int QuestIdConst = 19001;
    private const int CorneliusNpc = 203780;
    private const int VaterosNpc   = 798600;
    private const int ToolItemId   = 182206127;

    private readonly IItemDao _itemDao;

    public _19001ExpertEssencetappingExpert(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        var cornelius = engine.RegisterQuestNpc(CorneliusNpc);
        cornelius.OnQuestStart.Add(QuestId);
        cornelius.OnTalk.Add(QuestId);
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
            if (targetId != CorneliusNpc) return false;
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

            await GrantQuestSkillAsync(player, conn, 30002, 400, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, ToolItemId, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
