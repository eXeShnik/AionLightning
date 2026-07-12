// Port of Java data/scripts/system/handlers/quest/tiamaranta/_41573ACrystalofMamutProportions.java (madisson).
// Quest-item quest: using the crystal (182213170) opens the accept dialog (page 4); accept
// starts it; return the crystal to 205972 via SELECT_QUEST_REWARD to finish.
// Skip vs Java: the 3s SM_ITEM_USAGE_ANIMATION delay before showing the accept dialog is omitted
// (no such packet/scheduler hookup here) — purely cosmetic, the dialog still opens immediately.
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

namespace Quest.Tiamaranta;

public sealed class _41573ACrystalofMamutProportions : QuestHandlerBase
{
    private const int QuestIdConst  = 41573;
    private const int TurnInNpc     = 205972;
    private const int CrystalItemId = 182213170;

    private readonly IItemDao _itemDao;

    public _41573ACrystalofMamutProportions(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(CrystalItemId, QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != CrystalItemId) return false;
        await conn.SendAsync(new SM_DIALOG_WINDOW(0, 4, QuestId), ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == 0 && dialog == DialogAction.QUEST_ACCEPT_1)
        {
            await StartMissionAsync(conn, player, QuestStatus.START, ct);
            return await CloseDialogWindowAsync(conn, 0, ct);
        }

        if (entry is null || entry.Status == QuestStatus.NONE)
            return false;

        if (entry.Status == QuestStatus.START && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, CrystalItemId, 1, ct);
                await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
