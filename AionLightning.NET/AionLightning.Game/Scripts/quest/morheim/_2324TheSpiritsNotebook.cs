// Port of Java data/scripts/system/handlers/quest/morheim/_2324TheSpiritsNotebook.java.
// Item-use start (182204123); talk to 204373 (SELECT_QUEST_REWARD removes the notebook, sets
// var 1, flips to REWARD and completes in the same call).
// Deviation from Java: the source's onDialogEvent bails with `if (qs == null) return false;`
// before ever handling the QUEST_ACCEPT_1 accept branch, and onItemUseEvent never creates the
// quest state either — as written the quest could never actually start. Ported with the same
// item-use-then-accept convention used by every sibling script (e.g. _2316VivisBook) instead, so
// the quest is completable; this is treated as a latent bug in the original, not an intentional
// gate. Skip vs Java: the 3s SM_ITEM_USAGE_ANIMATION + scheduled follow-up dialog is collapsed
// into an immediate response (see _2316VivisBook for the same simplification).
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

namespace Quest.Morheim;

public sealed class _2324TheSpiritsNotebook : QuestHandlerBase
{
    private const int QuestIdConst  = 2324;
    private const int TurnInNpc     = 204373;
    private const int NotebookItemId = 182204123;

    private readonly IItemDao _itemDao;

    public _2324TheSpiritsNotebook(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(NotebookItemId, QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != NotebookItemId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is not null) return false;

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

        if (targetId == 0)
        {
            if (env.DialogId != (int)DialogAction.QUEST_ACCEPT_1) return false;
            await StartMissionAsync(conn, player, QuestStatus.START, ct);
            await conn.SendAsync(new SM_DIALOG_WINDOW(0, 0), ct);
            return true;
        }

        if (targetId == TurnInNpc && entry is { Status: QuestStatus.START })
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, NotebookItemId, 1, ct);
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
