// Port of Java data/scripts/system/handlers/quest/esoterrace/_28409GroupMaketheBladeComplete.java
// (Ritsu). Start at 799558; 799557 bumps var 0 -> 1 via SETPRO1 (no items); 205237 bumps var 0 -> 2
// via SETPRO2 (gives 182215007, consumes 182215006); kill 215795 at var 0 == 2 to flip to REWARD
// (giving 182215008, consuming 182215007); turn in at 799557. Unlike its Asmodian sibling
// (_18409GroupTiamatsPowerUnleashed), the item-relay npc (205237 here vs 205232 there) is actually
// registered via registerQuestNpc, so this quest is fully reachable end to end.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Esoterrace;

public sealed class _28409GroupMaketheBladeComplete : QuestHandlerBase
{
    private const int QuestIdConst = 28409;
    private const int StartNpc     = 799558;
    private const int RelayNpc     = 799557;
    private const int ItemRelayNpc = 205237;
    private const int KillMob      = 215795;
    private const int GiveItem     = 182215007;
    private const int RemoveItem   = 182215006;
    private const int RewardItem   = 182215008;

    private readonly IItemDao _itemDao;

    public _28409GroupMaketheBladeComplete(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RelayNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ItemRelayNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KillMob).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartNpc)
        {
            if (entry is null || entry.Status == QuestStatus.NONE)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId == RelayNpc)
        {
            if (entry is not null && entry.Status == QuestStatus.START && entry.GetVar(0) == 0)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (entry is not null && entry.Status == QuestStatus.REWARD)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId == ItemRelayNpc)
        {
            if (entry is not null && entry.Status == QuestStatus.START && entry.GetVar(0) == 1)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 1, 2, reward: false, sameNpc: false,
                        giveItemId: GiveItem, giveItemCount: 1, removeItemId: RemoveItem, removeItemCount: 1, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.TargetId != KillMob) return false;

        if (entry.GetVar(0) == 2)
        {
            await GiveQuestItemAsync(player, conn, _itemDao, RewardItem, 1, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, GiveItem, 1, ct);
            entry.Status = QuestStatus.REWARD;
            await UpdateQuestStatusAsync(conn, entry, ct);
        }
        return false;
    }
}
