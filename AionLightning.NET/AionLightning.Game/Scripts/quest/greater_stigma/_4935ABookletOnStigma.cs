// Port of Java data/scripts/system/handlers/quest/greater_stigma/_4935ABookletOnStigma.java
// (kecimis). Same shape as _3931HowToUseStigma: talk to Vergelmir (204051) to start; Teirunerk
// (204285) hands in the quest_data.xml collect-items to receive Teirunerk's Letter (182207107, var
// 1->2); Kohrunerk (279005, holding the letter) trades it for the Tattered Booklet (182207108) and
// flips to REWARD; turn in at Vergelmir (only reachable while carrying exactly one booklet). See
// _3931HowToUseStigma for the inner-switch-fallthrough translation and the dead registerQuestItem
// registration note (both apply identically here).
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.GreaterStigma;

public sealed class _4935ABookletOnStigma : QuestHandlerBase
{
    private const int QuestIdConst  = 4935;
    private const int VergelmirNpc  = 204051;
    private const int TeirunerkNpc  = 204285;
    private const int KohrunerkNpc  = 279005;
    private const int LetterItem    = 182207107;
    private const int BookletItem   = 182207108;

    private readonly IItemDao _itemDao;

    public _4935ABookletOnStigma(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(VergelmirNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestItem(LetterItem, QuestId);
        engine.RegisterQuestItem(BookletItem, QuestId);
        engine.RegisterQuestNpc(TeirunerkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KohrunerkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(VergelmirNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != VergelmirNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != VergelmirNpc || (player.Inventory.FindByItemId(BookletItem)?.Count ?? 0) != 1)
                return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        if (entry.Status != QuestStatus.START) return false;

        if (targetId == TeirunerkNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.QUEST_SELECT && var == 1)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (env.DialogId == (int)DialogAction.CHECK_USER_HAS_QUEST_ITEM && var == 1)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 2, reward: false, checkOkId: 10000, checkFailId: 10001, giveItemId: LetterItem, giveItemCount: 1, ct);
            if (dialog == DialogAction.SETPRO1
                || (dialog == DialogAction.QUEST_SELECT && var != 0 && var != 1)
                || (env.DialogId == (int)DialogAction.CHECK_USER_HAS_QUEST_ITEM && var != 1))
            {
                if (var == 0) entry.SetVar(0, 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
            return false;
        }

        if (targetId == KohrunerkNpc && (player.Inventory.FindByItemId(LetterItem)?.Count ?? 0) == 1)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 2)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SET_SUCCEED || (dialog == DialogAction.QUEST_SELECT && var != 2))
            {
                if (var == 2)
                    await RemoveQuestItemAsync(player, conn, _itemDao, LetterItem, 1, ct);
                await GiveQuestItemAsync(player, conn, _itemDao, BookletItem, 1, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
            return false;
        }

        return false;
    }
}
