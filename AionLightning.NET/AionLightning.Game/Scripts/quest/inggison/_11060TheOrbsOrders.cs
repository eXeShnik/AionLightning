// Port of Java data/scripts/system/handlers/quest/inggison/_11060TheOrbsOrders.java.
// Item-use start (182206846, no removal on accept); relay at 799010 (var 0->1); kill 218756 once
// while var==1 flips to reward; turn in at the mob's corpse (218756) itself, removing the item.
// Java bug fixed: register() calls addOnKillEvent(218756) but never addOnTalkEvent(218756), even
// though onDialogEvent's REWARD branch checks targetId==218756 for the turn-in — Java's own
// QuestEngine.onDialog dispatches purely off each npc's registered OnTalk list (verified against
// QuestEngine.java), so that branch was dead/unreachable in the original, permanently stranding
// the quest after the kill. Fixed by also registering 218756 for OnTalk here.
// Also faithfully preserved: register() lists 799015 for OnTalk but no dialog branch ever checks
// that target id — inert but harmless, kept for parity with Java's own dead registration.
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

namespace Quest.Inggison;

public sealed class _11060TheOrbsOrders : QuestHandlerBase
{
    private const int QuestIdConst = 11060;
    private const int RelayNpc     = 799010;
    private const int InertNpc     = 799015;
    private const int TargetMob    = 218756;
    private const int TokenItem    = 182206846;

    private readonly IItemDao _itemDao;

    public _11060TheOrbsOrders(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(TokenItem, QuestId);
        engine.RegisterQuestNpc(RelayNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(InertNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TargetMob).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(TargetMob).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != TokenItem) return false;
        if (player.Quests.Get(QuestId) is not null) return false;
        await conn.SendAsync(new SM_DIALOG_WINDOW(0, 4, QuestId), ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == 0 && env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await CloseDialogWindowAsync(conn, 0, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == RelayNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TargetMob)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, TokenItem, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.TargetId != TargetMob || entry.GetVar(0) != 1) return false;
        await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
        return true;
    }
}
