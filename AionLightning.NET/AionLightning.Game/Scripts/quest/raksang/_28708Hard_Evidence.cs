// Port of Java data/scripts/system/handlers/quest/raksang/_28708Hard_Evidence.java.
// Asmodian mirror of _18708Sealed_With_The_Truth: item-use start (182212213, no start npc), talk
// to npc 799435, same raw-dialog-id-26/SELECT_QUEST_REWARD consume-and-reward flow.
// Java bug fixed: onItemUseEvent checked item id 182212213 -- wait, checked the WRONG id, 182212206
// (the Elyos counterpart's Sealing Stone from _18708), even though register() wires up and
// onDialogEvent correctly removes 182212213 as this quest's own item. That copy-paste mismatch
// means the offer dialog would only ever fire for a Sealing Stone this Asmodian quest never grants
// (182212206 belongs to the other faction's quest) -- fixed to check 182212213 here.
// Skip vs Java: same item-usage-animation-broadcast skip as _18708Sealed_With_The_Truth.
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

namespace Quest.Raksang;

public sealed class _28708Hard_Evidence : QuestHandlerBase
{
    private const int QuestIdConst      = 28708;
    private const int EinNpc            = 799435;
    private const int SealingStoneItem  = 182212213;

    private readonly IItemDao _itemDao;

    public _28708Hard_Evidence(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(EinNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(SealingStoneItem, QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(0, 0), ct);
                return true;
            }
            await conn.SendAsync(new SM_DIALOG_WINDOW(0, 0), ct);
        }

        if (entry is null) return false;

        int var = entry.GetVar(0);
        if (entry.Status == QuestStatus.START && env.TargetId == EinNpc)
        {
            int targetObjId = env.Target?.ObjectId ?? 0;
            if (env.DialogId == 26 && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (env.DialogId == 26 || env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, SealingStoneItem, 1, ct);
                return await DefaultCloseDialogAsync(env, conn, 0, 1, reward: true, sameNpc: true, ct);
            }
        }

        // Java bug: falls through to `return defaultCloseDialog(env, 799435, 2375);` here — dead
        // code identical to _18708Sealed_With_The_Truth's copy of the same file. Preserved as false.
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != SealingStoneItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status == QuestStatus.NONE)
            await conn.SendAsync(new SM_DIALOG_WINDOW(0, 4, QuestId), ct);
        return true;
    }
}
