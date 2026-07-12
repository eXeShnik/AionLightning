// Port of Java data/scripts/system/handlers/quest/raksang/_18708Sealed_With_The_Truth.java.
// Item-use start (Sealing Stone 182212206, no start npc): using the stone pops the targetId==0
// offer dialog (page 4) when not started; accepting starts the quest and closes the dialog. Talk
// to Ein (799435): raw dialog id 26 shows the confirm page (2375) at var 0, and both raw id 26
// (falling through when var != 0, matching Java's case fallthrough) and SELECT_QUEST_REWARD (1009)
// consume the Sealing Stone and flip straight to REWARD via the same npc's end dialog.
// Skip vs Java: the SM_ITEM_USAGE_ANIMATION broadcast on item use is dropped (same cosmetic skip
// already established for _1845OpeningDoors/_1107TheLostAxe) — the dialog opens immediately.
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

public sealed class _18708Sealed_With_The_Truth : QuestHandlerBase
{
    private const int QuestIdConst      = 18708;
    private const int EinNpc            = 799435;
    private const int SealingStoneItem  = 182212206;

    private readonly IItemDao _itemDao;

    public _18708Sealed_With_The_Truth(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
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

        // Java bug: falls through to `return defaultCloseDialog(env, 799435, 2375);` here, reading
        // the npc id 799435 as a quest-var "step" that var 0 (clamped 0-63) can never reach — the
        // call is dead code and always resolves to false. Preserved as an explicit false.
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
