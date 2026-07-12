// Port of Java data/scripts/system/handlers/quest/crafting/_29001ExpertEssencetappingExpert.java (Gigi).
// Asmodian mirror of _19001ExpertEssencetappingExpert: Latatusk (204096) hands over the tapping
// tool (182207141) on dialog open; Vateros (798800) completes the quest and, in Java, teaches
// skill 30002 level 400.
// Skip vs Java: the addSkill(30002, 400) grant is omitted — see _19001ExpertEssencetappingExpert
// for the reason (no ISkillDao reachable from this batch's fixed-shape script constructor).
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

public sealed class _29001ExpertEssencetappingExpert : QuestHandlerBase
{
    private const int QuestIdConst = 29001;
    private const int LatatuskNpc = 204096;
    private const int VaterosNpc  = 798800;
    private const int ToolItemId  = 182207141;

    private readonly IItemDao _itemDao;

    public _29001ExpertEssencetappingExpert(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        var latatusk = engine.RegisterQuestNpc(LatatuskNpc);
        latatusk.OnQuestStart.Add(QuestId);
        latatusk.OnTalk.Add(QuestId);
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
            if (targetId != LatatuskNpc) return false;
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

            await RemoveQuestItemAsync(player, conn, _itemDao, ToolItemId, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
