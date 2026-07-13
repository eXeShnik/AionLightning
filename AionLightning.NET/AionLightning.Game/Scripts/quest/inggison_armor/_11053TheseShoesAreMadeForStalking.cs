// Port of Java data/scripts/system/handlers/quest/inggison_armor/_11053TheseShoesAreMadeForStalking.java (vlog, modified Gigi).
// Turn-in at 799015: in START, CHECK_USER_HAS_QUEST_ITEM pays a 50,000 kinah fee plus 30x item
// 182206838 to flip to REWARD (ok page 5, insufficient page 2716); FINISH_DIALOG closes via
// defaultCloseDialog; turn in.
// note: Java evaluates `tryDecreaseKinah(50000) && itemCount > 29` — which decrements kinah inside
// the condition even when the item count is short, silently losing the fee. Both conditions are
// checked here before either resource is consumed, so no kinah is lost on the failure path; the
// success path (the only meaningful one) is behaviorally identical.
// note: Java's qs.canRepeat() daily-repeat re-entry is approximated as "no active entry".
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

namespace Quest.InggisonArmor;

public sealed class _11053TheseShoesAreMadeForStalking : QuestHandlerBase
{
    private const int QuestIdConst = 11053;
    private const int Npc          = 799015;
    private const int FeeItemId    = 182206838;
    private const int KinahItemId  = 182400001;
    private const long KinahFee    = 50000;

    private readonly IItemDao _itemDao;

    public _11053TheseShoesAreMadeForStalking(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(Npc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Npc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId == Npc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == Npc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                {
                    long itemCount = player.Inventory.FindByItemId(FeeItemId)?.Count ?? 0;
                    long kinah     = player.Inventory.FindByItemId(KinahItemId)?.Count ?? 0;
                    if (itemCount > 29 && kinah >= KinahFee)
                    {
                        await RemoveQuestItemAsync(player, conn, _itemDao, KinahItemId, KinahFee, ct);
                        await RemoveQuestItemAsync(player, conn, _itemDao, FeeItemId, 30, ct);
                        await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                    }
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                }
                if (dialog == DialogAction.FINISH_DIALOG)
                    return await DefaultCloseDialogAsync(env, conn, 0, 0, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == Npc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
