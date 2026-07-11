// Port of Java data/scripts/system/handlers/quest/verteron/_1182AncientStoneFragment.java
// (MrPoke, modified Nephis). Using the Ancient Stone Fragment (182200549) opens the accept
// dialog; accepting starts the quest; turn in at Krotan (203099).
// Skip vs Java: the 3s SM_ITEM_USAGE_ANIMATION cast delay before showing dialog 4 is collapsed
// to an immediate dialog open (no animation-scheduling infra ported yet, matches
// UseQuestObjectAsync's documented simplification).
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Verteron;

public sealed class _1182AncientStoneFragment : QuestHandlerBase
{
    private const int QuestIdConst = 1182;
    private const int KrotanNpc    = 203099;
    private const int FragmentItem = 182200549;

    private readonly IItemDao _itemDao;

    public _1182AncientStoneFragment(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(KrotanNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(FragmentItem, QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != FragmentItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null)
            await conn.SendAsync(new SM_DIALOG_WINDOW(0, 4, QuestId), ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;

        if (targetId == 0)
        {
            if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(0, 0), ct);
                return true;
            }
            return false;
        }

        if (targetId == KrotanNpc && entry is not null)
        {
            int targetObjId = env.Target?.ObjectId ?? 0;
            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT && entry.Status == QuestStatus.START)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);

            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD
                && entry.Status != QuestStatus.COMPLETE && entry.Status != QuestStatus.NONE)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, FragmentItem, 1, ct);
                entry.SetVar(0, 1);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
