// Port of Java data/scripts/system/handlers/quest/verteron/_1198TheWritingOnTheWall.java
// (Cheatkiller). Talking to the Ancient Wall Writing (700009) opens the accept dialog (targetId
// 0); accepting starts the quest and gives item 182200559; turn in the item at Krotan (203098),
// which removes an unrelated tracker item (182213168) and advances/rewards in the same step
// (Java's own defaultCloseDialog(..., sameNpc: true) call).
using System.Linq;
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

namespace Quest.Verteron;

public sealed class _1198TheWritingOnTheWall : QuestHandlerBase
{
    private const int QuestIdConst  = 1198;
    private const int WallObj       = 700009;
    private const int KrotanNpc     = 203098;
    private const int LetterItemId  = 182200559;
    private const int TrackerItemId = 182213168;

    private readonly IItemDao _itemDao;

    public _1198TheWritingOnTheWall(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(LetterItemId, QuestId);
        engine.RegisterQuestNpc(WallObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KrotanNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != LetterItemId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null)
            await conn.SendAsync(new SM_DIALOG_WINDOW(0, 4, QuestId), ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;

        if (entry is null)
        {
            if (targetId == 0)
            {
                if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
                {
                    await StartMissionAsync(conn, player, QuestStatus.START, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(0, 0), ct);
                    return true;
                }
                return false;
            }
            if (targetId == WallObj)
                return await GiveQuestItemAsync(player, conn, _itemDao, LetterItemId, 1, ct);
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == KrotanNpc)
        {
            int targetObjId = env.Target?.ObjectId ?? 0;
            var dialog = DialogActionLookup.FromId(env.DialogId);
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, TrackerItemId, 1, ct);
                return await DefaultCloseDialogAsync(env, conn, 0, 1, reward: true, sameNpc: true, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == KrotanNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
