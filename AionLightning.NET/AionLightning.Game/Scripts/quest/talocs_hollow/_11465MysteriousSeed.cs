// Port of Java data/scripts/system/handlers/quest/talocs_hollow/_11465MysteriousSeed.java (madison).
// Item-driven quest, no start npc: accepting/refusing goes through the targetId==0 QUEST_ACCEPT_1/
// QUEST_REFUSE_1 dialog path (same shape as beluslan._2670TheAncientBook). Using item 182209523
// while at var 0 opens the examine dialog (2375) at npc 279000, which self-turns-in via
// SELECT_QUEST_REWARD (removes the item, flips to REWARD, shows dialog 5); the next visit to
// 279000 shows the standard reward window.
// Skip vs Java: OnItemUseAsync's 3s SM_ITEM_USAGE_ANIMATION broadcast before showing dialog 4 is
// dropped as cosmetic-only, same simplification as beluslan._2670TheAncientBook / inggison._11033.
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

namespace Quest.TalocsHollow;

public sealed class _11465MysteriousSeed : QuestHandlerBase
{
    private const int QuestIdConst = 11465;
    private const int ExamineNpc   = 279000;
    private const int SeedItem     = 182209523;

    private readonly IItemDao _itemDao;

    public _11465MysteriousSeed(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(SeedItem, QuestId);
        engine.RegisterQuestNpc(ExamineNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == 0)
        {
            if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(0, 0), ct);
                return true;
            }
            if (env.DialogId == (int)DialogAction.QUEST_REFUSE_1)
            {
                await conn.SendAsync(new SM_DIALOG_WINDOW(0, 0), ct);
                return true;
            }
        }

        if (entry is null || entry.Status == QuestStatus.NONE) return false;

        if (entry.Status == QuestStatus.START && targetId == ExamineNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, SeedItem, 1, ct);
                await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == ExamineNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != SeedItem) return false;
        await conn.SendAsync(new SM_DIALOG_WINDOW(0, 4, QuestId), ct);
        return true;
    }
}
