// Port of Java data/scripts/system/handlers/quest/carving_fortune/_2097SpiritBlade.java
// (Mr. Poke, dune11). Talk to Munin (203550, var 0->1), Skulid (203546, var 1->2), then collect the
// <collect_item> quest_data.xml materials at 279034 (var 2 -> REWARD, gives item 182207085).
// Skip vs Java: the hand-rolled collect check manually calls QuestService.collectItemCheck +
// giveQuestItem + an extra intermediate SM_DIALOG_WINDOW(target,10) refresh packet before the final
// dialog page - this port reuses the shared CheckQuestItemsAsync helper (same helper already used
// by e.g. the_circle's WardsAndWardOrbs quests) instead of hand-rolling it again, which folds the
// give-item + step/status change into one call and skips that purely cosmetic extra packet.
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

namespace Quest.CarvingFortune;

public sealed class _2097SpiritBlade : QuestHandlerBase
{
    private const int QuestIdConst = 2097;
    private const int MuninNpc     = 203550;
    private const int SkulidNpc    = 203546;
    private const int CollectNpc   = 279034;
    private const int RewardItem   = 182207085;

    private readonly IItemDao _itemDao;

    public _2097SpiritBlade(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(MuninNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SkulidNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(CollectNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(RewardItem, QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);
        int var         = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            switch (targetId)
            {
                case MuninNpc:
                    if (dialog == DialogAction.QUEST_SELECT && var == 0)
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (dialog == DialogAction.SETPRO1)
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                    return false;

                case SkulidNpc:
                    if (dialog == DialogAction.QUEST_SELECT)
                    {
                        if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                        return true;
                    }
                    if (dialog == DialogAction.SETPRO2)
                        return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                    return false;

                case CollectNpc:
                    if (dialog == DialogAction.QUEST_SELECT && var == 2)
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                        return await CheckQuestItemsAsync(env, conn, _itemDao, 2, 2, reward: true, 10000, 10001,
                            RewardItem, 1, ct);
                    return false;

                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == MuninNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
