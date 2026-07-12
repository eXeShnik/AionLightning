// Port of Java data/scripts/system/handlers/quest/sarpan/_41350CookingWithDratamin.java (Cheatkiller).
// Talk to 701440 to receive the ingredient (item 182213112, no quest state required yet); use the
// item to open the accept dialog (no start npc); turn in at 205587 (var==0, SELECT_QUEST_REWARD
// flips to REWARD, same npc; the final visit removes the item and finishes).
// Skip vs Java: qs.canRepeat() (daily-repeat gate on the ingredient hand-out) isn't ported — same
// simplification as the rest of this port (treated as "no active entry").
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

namespace Quest.Sarpan;

public sealed class _41350CookingWithDratamin : QuestHandlerBase
{
    private const int QuestIdConst = 41350;
    private const int IngredientNpc = 701440;
    private const int TurnInNpc     = 205587;
    private const int ItemId        = 182213112;

    private readonly IItemDao _itemDao;

    public _41350CookingWithDratamin(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(ItemId, QuestId);
        engine.RegisterQuestNpc(IngredientNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != ItemId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status == QuestStatus.NONE)
            return await SendQuestDialogAsync(conn, 0, 4, ct);
        return false;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == 0 && env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(0, 0), ct);
                return true;
            }
            if (targetId == IngredientNpc)
                return await GiveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, reward: true, sameNpc: true, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
