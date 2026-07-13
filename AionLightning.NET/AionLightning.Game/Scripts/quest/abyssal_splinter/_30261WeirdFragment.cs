// Port of Java data/scripts/system/handlers/quest/abyssal_splinter/_30261WeirdFragment.java
// (Rikka). Using item 182209800 starts the quest directly (Java QuestService.startQuest(env) from
// onItemUseEvent, no NPC-talk accept - ported via StartMissionAsync same as
// idian_depths/_23617TomboftheElder.cs), consumes the item, and opens dialog 4; report to Rentia
// (278533) then Lugbug (279029) to advance var 0->1->2 (reward flip at Lugbug); turn in at Aratus
// (260264).
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

namespace Quest.AbyssalSplinter;

public sealed class _30261WeirdFragment : QuestHandlerBase
{
    private const int QuestIdConst = 30261;
    private const int RentiaNpc    = 278533;
    private const int LugbugNpc    = 279029;
    private const int TurnInNpc    = 260264;
    private const int ItemId       = 182209800;

    private readonly IItemDao _itemDao;

    public _30261WeirdFragment(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(RentiaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(LugbugNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(ItemId, QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status == QuestStatus.NONE) return false;

        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (env.TargetId == RentiaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (env.TargetId == LugbugNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, reward: true, sameNpc: false, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && env.TargetId == TurnInNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != ItemId) return false;
        if (player.Quests.Get(QuestId) is not null) return false;

        await StartMissionAsync(conn, player, QuestStatus.START, ct);
        await RemoveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
        return await SendQuestDialogAsync(conn, 0, 4, ct);
    }
}
