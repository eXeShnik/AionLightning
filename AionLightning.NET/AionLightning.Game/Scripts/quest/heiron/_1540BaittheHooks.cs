// Port of Java data/scripts/system/handlers/quest/heiron/_1540BaittheHooks.java.
// Accepting from Rotgut (204588) hands over the bait item 182201822 if not already carried; use
// the three hooks (730189/730190/730191) in sequence while holding it (var0->1->2->3, the third
// use consumes the bait and flips straight to REWARD); turn in at Rotgut.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Heiron;

public sealed class _1540BaittheHooks : QuestHandlerBase
{
    private const int QuestIdConst = 1540;
    private const int StartNpc  = 204588;
    private const int HookOne   = 730189;
    private const int HookTwo   = 730190;
    private const int HookThree = 730191;
    private const int BaitItem  = 182201822;

    private readonly IItemDao _itemDao;

    public _1540BaittheHooks(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HookOne).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HookTwo).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HookThree).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.QUEST_ACCEPT_1 && player.Inventory.FindByItemId(BaitItem) is null or { Count: 0 })
                {
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, BaitItem, 1, ct)) return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry is null) return false;

        if (entry.Status == QuestStatus.START)
        {
            bool hasBait = player.Inventory.FindByItemId(BaitItem) is { Count: 1 };
            if (targetId == HookOne && dialog == DialogAction.USE_OBJECT && hasBait)
                return await UseQuestObjectAsync(env, conn, 0, 1, false, 0, ct);
            if (targetId == HookTwo && dialog == DialogAction.USE_OBJECT && hasBait)
                return await UseQuestObjectAsync(env, conn, 1, 2, false, 0, ct);
            if (targetId == HookThree && dialog == DialogAction.USE_OBJECT && entry.GetVar(0) == 2 && hasBait)
            {
                entry.SetVar(0, 3);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                await RemoveQuestItemAsync(player, conn, _itemDao, BaitItem, 1, ct);
                return true;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }
}
