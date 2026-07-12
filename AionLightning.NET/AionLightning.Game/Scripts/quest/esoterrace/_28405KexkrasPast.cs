// Port of Java data/scripts/system/handlers/quest/esoterrace/_28405KexkrasPast.java (Vincas).
// Elyos mirror of _18405MemoriesInTheCornerOfHisMind: started purely by using quest item 182215014
// (target 0, QUEST_ACCEPT_1 creates the entry); Luigur (799558) hands out a note item at var 0
// (182215025, removing the memory item 182215014), Relyt (799557) takes it back at var 1 and
// completes. Java's onDialogEvent switches on targetId with NO break between the two npc cases, so
// the var==1 completion logic (case npcRelyt) is reachable both by talking to Relyt directly AND by
// talking to Luigur again once var is 1 - reproduced explicitly rather than relying on C# switch
// fallthrough. Skip vs Java: the SM_ITEM_USAGE_ANIMATION broadcast + 3s scheduled delay before
// showing the accept dialog are dropped (same cosmetic skip as other item-started quests in this
// port) - the dialog opens immediately on item use instead.
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

namespace Quest.Esoterrace;

public sealed class _28405KexkrasPast : QuestHandlerBase
{
    private const int QuestIdConst = 28405;
    private const int NpcLuigur    = 799558;
    private const int NpcRelyt     = 799557;
    private const int NoteItem     = 182215025;
    private const int MemoryItem   = 182215014;

    private readonly IItemDao _itemDao;

    public _28405KexkrasPast(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(NpcLuigur).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(NpcRelyt).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(MemoryItem, QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != MemoryItem) return false;
        await SendQuestDialogAsync(conn, 0, 4, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (targetId == 0 && dialog == DialogAction.QUEST_ACCEPT_1)
        {
            await StartMissionAsync(conn, player, QuestStatus.START, ct);
            return await CloseDialogWindowAsync(conn, 0, ct);
        }

        var entry = player.Quests.Get(QuestId);
        if (entry is null) return false;

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId == NpcLuigur && var == 0)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 0, 1, reward: false, sameNpc: false,
                        giveItemId: NoteItem, giveItemCount: 1, removeItemId: MemoryItem, removeItemCount: 1, ct);
            }

            // Java switch fallthrough: reachable via NpcLuigur (when var==0 check above didn't match
            // and dialog itself is a no-op) or directly via NpcRelyt.
            if ((targetId == NpcLuigur || targetId == NpcRelyt) && var == 1)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    await RemoveQuestItemAsync(player, conn, _itemDao, NoteItem, 1, ct);
                return await DefaultCloseDialogAsync(env, conn, 1, 2, reward: true, sameNpc: true, ct);
            }
        }

        return await SendQuestRewardDialogAsync(env, conn, ct);
    }

    /// <summary>Java sendQuestRewardDialog(env, npcRelyt, 0): the turn-in fallback tried once none of
    /// the step checks above match.</summary>
    private async ValueTask<bool> SendQuestRewardDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is not { Status: QuestStatus.REWARD } || env.TargetId != NpcRelyt) return false;

        int targetObjId = env.Target?.ObjectId ?? 0;
        if (DialogActionLookup.FromId(env.DialogId) == DialogAction.USE_OBJECT)
            return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
        return await SendQuestEndDialogAsync(env, conn, ct);
    }
}
