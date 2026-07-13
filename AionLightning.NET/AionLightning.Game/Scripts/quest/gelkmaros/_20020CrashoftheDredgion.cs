// Port of Java data/scripts/system/handlers/quest/gelkmaros/_20020CrashoftheDredgion.java (Gigi, vlog).
// Zone-mission opener of the Gelkmaros campaign: Richelle (799225, var0->1) -> Valetta (799226,
// relays var1->2, CHECK_USER_HAS_QUEST_ITEM collect-check var2->3, var5->6, var7->8, plus the
// turn-in) -> Vellun (799239, var3->4) -> Heszti (798703, var4->5, gives item 182207600) -> using
// that item inside DF4_ITEMUSEAREA_Q20020 (var6->7) -> entering Coward's Cove at var8 flips to
// REWARD; turn in back at Valetta. NPC 700977 is an unconditional no-op click target (Java
// switch case with no dialog guard) and NPC 799342's OnKill registration is dead — the Java class
// never overrides onKillEvent, so it's registered here for parity but has no handler.
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

namespace Quest.Gelkmaros;

public sealed class _20020CrashoftheDredgion : QuestHandlerBase
{
    private const int QuestIdConst   = 20020;
    private const int RichelleNpc    = 799225;
    private const int ValettaNpc     = 799226;
    private const int VellunNpc      = 799239;
    private const int HesztiNpc      = 798703;
    private const int WreckObject    = 700977;
    private const int DeadDropNpc    = 799342;
    private const int GivenItem      = 182207600;
    private const string ItemUseZone = "DF4_ITEMUSEAREA_Q20020";
    private const string RewardZone  = "COWARDS_COVE_220070000";
    private static readonly int[] _talkNpcs = [RichelleNpc, ValettaNpc, VellunNpc, HesztiNpc, WreckObject];

    private readonly IItemDao _itemDao;

    public _20020CrashoftheDredgion(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(DeadDropNpc).OnKill.Add(QuestId);
        engine.RegisterQuestItem(GivenItem, QuestId);
        RegisterOnEnterZone(engine, RewardZone);
        foreach (int npc in _talkNpcs) engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 20000, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId == WreckObject)
                return true; // Java: unconditional handled click, no state change

            if (targetId == RichelleNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }

            if (targetId == ValettaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (var == 5) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                    if (var == 7) return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 2, 3, false, 10000, 10001, ct);
                if (dialog == DialogAction.SETPRO6)
                    return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
                if (dialog == DialogAction.SETPRO8)
                    return await DefaultCloseDialogAsync(env, conn, 7, 8, ct);
                if (dialog == DialogAction.FINISH_DIALOG)
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                return false;
            }

            if (targetId == VellunNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 3)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO4)
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                return false;
            }

            if (targetId == HesztiNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 4)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO5)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 4, 5, reward: false, sameNpc: false,
                        GivenItem, 1, 0, 0, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == ValettaNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (!player.CurrentZones.Contains(ItemUseZone)) return false;

        var env = new QuestEnv(null, player, QuestId, 0);
        return await UseQuestObjectAsync(env, conn, 6, 7, reward: false, varNum: 0,
            addItemId: 0, addItemCount: 0, removeItemId: itemId, removeItemCount: 1,
            movieId: 0, dieObject: false, _itemDao, ct);
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != RewardZone) return false;

        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) != 8) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 8, toReward: true, ct);
        return true;
    }
}
