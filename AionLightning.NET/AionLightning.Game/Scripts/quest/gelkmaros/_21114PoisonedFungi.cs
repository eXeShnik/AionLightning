// Port of Java data/scripts/system/handlers/quest/gelkmaros/_21114PoisonedFungi.java (Cheatkiller).
// Talk to 799282 to start; examine fungi patches 700727/700728 at var 0 (no-op acknowledgement);
// collect-check turns in quest items at var 0->1; report at 799282 gives item 182207862 (var 1->2);
// use 700729 at var 2 (removes the item, var 2->3, no reward); back at 799282 (var 3->4); kill
// 216563 while at var 4 flips to REWARD; turn in at 799282.
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

namespace Quest.Gelkmaros;

public sealed class _21114PoisonedFungi : QuestHandlerBase
{
    private const int QuestIdConst = 21114;
    private const int StartNpc     = 799282;
    private const int FungiA       = 700727;
    private const int FungiB       = 700728;
    private const int FungiC       = 700729;
    private const int MobId        = 216563;
    private const int ItemId       = 182207862;

    private readonly IItemDao _itemDao;

    public _21114PoisonedFungi(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(FungiA).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FungiC).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FungiB).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(799405).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MobId).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    return false;
                }
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 1, reward: false, checkOkId: 10000, checkFailId: 10001, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                }
                if (dialog == DialogAction.SETPRO4)
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                return false;
            }

            if ((targetId == FungiA || targetId == FungiB) && var == 0)
                return true;

            if (targetId == FungiC && var == 2)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
                await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
                return true;
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, MobId, startVar: 4, reward: true, ct);
}
