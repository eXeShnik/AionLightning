// Port of Java data/scripts/system/handlers/quest/pandaemonium/_2918DeepMaternalLove.java.
// Item-use start (182207009, no npc target): using it before the quest exists shows dialog page 4;
// accepting (targetId 0, QUEST_ACCEPT_1) starts the quest directly (Java QuestService.startQuest).
// Turn in at 203574: SELECT_QUEST_REWARD removes the item, flips to REWARD, and immediately shows
// the end dialog; any other click while active also routes to the end dialog (Java's unconditional
// else branch — harmless no-op via SendQuestEndDialogAsync's own status guard).
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

namespace Quest.Pandaemonium;

public sealed class _2918DeepMaternalLove : QuestHandlerBase
{
    private const int QuestIdConst = 2918;
    private const int Npc = 203574;
    private const int QuestItem = 182207009;

    private readonly IItemDao _itemDao;

    public _2918DeepMaternalLove(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(QuestItem, QuestId);
        engine.RegisterQuestNpc(Npc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != QuestItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is not null && entry.Status != QuestStatus.NONE) return false;

        await conn.SendAsync(new SM_DIALOG_WINDOW(0, 4, QuestId), ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if ((entry is null || entry.Status == QuestStatus.NONE) && targetId == 0)
        {
            if (dialog == DialogAction.QUEST_ACCEPT_1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await CloseDialogWindowAsync(conn, 0, ct);
            }
            return false;
        }

        if (targetId == Npc && entry is not null)
        {
            if (dialog == DialogAction.QUEST_SELECT && entry.Status == QuestStatus.START)
                return await SendQuestDialogAsync(conn, env.Target?.ObjectId ?? 0, 2375, ct);

            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, QuestItem, 1, ct);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
