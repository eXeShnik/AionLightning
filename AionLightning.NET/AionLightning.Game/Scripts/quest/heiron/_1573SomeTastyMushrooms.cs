// Port of Java data/scripts/system/handlers/quest/heiron/_1573SomeTastyMushrooms.java.
// Talk to 730025 to start; a collect-check gives a raw mushroom (182201784, var0->1); using it
// (any interaction with 700194 is a no-op acknowledgement) removes it and gives the grown mushroom
// (182201735, var1->2); back at 730025, hand it in for REWARD.
// Skip vs Java: the item-use zone check (LF3_ITEMUSEAREA_Q1573) isn't ported (no zone-shape infra
// yet) — the item is usable anywhere; the give/remove/step-transition logic is unaffected.
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

namespace Quest.Heiron;

public sealed class _1573SomeTastyMushrooms : QuestHandlerBase
{
    private const int QuestIdConst = 1573;
    private const int StartNpc  = 730025;
    private const int SpotNpc   = 700194;
    private const int RawMushroomItem   = 182201784;
    private const int GrownMushroomItem = 182201735;

    private readonly IItemDao _itemDao;

    public _1573SomeTastyMushrooms(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(RawMushroomItem, QuestId);
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SpotNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != RawMushroomItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, RawMushroomItem, 1, ct);
        var env = new QuestEnv(null, player, QuestId, 0);
        return await UseQuestObjectAsync(env, conn, 1, 2, false, 0, GrownMushroomItem, 1, 0, 0, 0, false, _itemDao, ct);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var entry  = player.Quests.Get(QuestId);
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == SpotNpc) return true;
            if (targetId == StartNpc)
            {
                int var = entry.GetVar(0);
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 2)
                    {
                        await RemoveQuestItemAsync(player, conn, _itemDao, GrownMushroomItem, 1, ct);
                        entry.Status = QuestStatus.REWARD;
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                    }
                    return false;
                }
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 1, false, 0, 10001, RawMushroomItem, 1, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == StartNpc) return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
