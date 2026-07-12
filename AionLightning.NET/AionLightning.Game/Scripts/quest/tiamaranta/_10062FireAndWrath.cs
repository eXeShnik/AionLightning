// Port of Java data/scripts/system/handlers/quest/tiamaranta/_10062FireAndWrath.java (vlog).
// Talk to Adella (205886) to advance var 0->1 (SETPRO1); entering WRATH_VENT flips 1->2; kill
// 218766 to flip 2->3; picking up quest-item 182212556 (100% drop off 701136 at step 3) flips
// straight to REWARD; turn in at Garnon (800018), which also removes the item on the default
// (non-USE_OBJECT) branch.
// Skip vs Java: onLogOutEvent has no equivalent hook in this port (no OnLogOut in IQuestHandler) -
// omitted. Functionally redundant anyway: onGetItemEvent already completes the quest the instant the
// item is picked up from the mob drop, which is the only way var reaches 3 with the item in hand.
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

namespace Quest.Tiamaranta;

public sealed class _10062FireAndWrath : QuestHandlerBase
{
    private const int QuestIdConst = 10062;
    private const int AdellaNpc    = 205886;
    private const int GarnonNpc    = 800018;
    private const int KillNpc      = 218766;
    private const int DropNpc      = 701136;
    private const int TokenItemId  = 182212556;
    private const string WrathVentZone = "WRATH_VENT_600030000";

    private readonly IItemDao _itemDao;

    public _10062FireAndWrath(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        RegisterQuestDrop(engine, DropNpc, TokenItemId, 1, 100, 3);
        engine.RegisterQuestNpc(AdellaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GarnonNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KillNpc).OnKill.Add(QuestId);
        engine.RegisterQuestItem(TokenItemId, QuestId);
        RegisterOnEnterZone(engine, WrathVentZone);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START && targetId == AdellaNpc)
        {
            int var = entry.GetVar(0);
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == GarnonNpc)
        {
            if (dialog == DialogAction.USE_OBJECT && entry.GetVar(0) == 3)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, TokenItemId, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != WrathVentZone) return false;
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
        return true;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, KillNpc, 2, 3, ct);

    public override async ValueTask<bool> OnItemGetAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != TokenItemId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 3) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: true, ct);
        return true;
    }
}
