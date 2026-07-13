// Port of Java data/scripts/system/handlers/quest/tiamaranta/_41514BarrenProspects.java (mr.madison).
// Talk to 205935 to accept (grants quest item 182212521 via the established QUEST_ACCEPT_SIMPLE +
// GiveQuestItemAsync precedent); using that item while inside LDF4B_ITEMUSEAREA_Q41514A flips the
// quest straight to REWARD; turn in at 205935.
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

namespace Quest.Tiamaranta;

public sealed class _41514BarrenProspects : QuestHandlerBase
{
    private const int QuestIdConst = 41514;
    private const int StartNpc     = 205935;
    private const int ItemId       = 182212521;
    private const string ItemUseZone = "LDF4B_ITEMUSEAREA_Q41514A";

    private readonly IItemDao _itemDao;

    public _41514BarrenProspects(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(ItemId, QuestId);
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
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
                await GiveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (itemId != ItemId || !player.CurrentZones.Contains(ItemUseZone)) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
        return true;
    }
}
