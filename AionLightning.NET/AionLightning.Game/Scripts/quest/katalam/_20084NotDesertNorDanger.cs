// Port of Java data/scripts/system/handlers/quest/katalam/_20084NotDesertNorDanger.java (Cheatkiller, apozema).
// Asmodian mirror of 10084: Tolkin(800553) -> kills 230405/230406 -> Jeminu(800554 gives item) ->
// use item (var 8->9) -> Theodrik(801153) -> Beritra body(800555) -> onAtDistance near 206285/231222
// (var 11->12) -> Otro(800565) -> Broken Sword(701538) -> turn in Niel(800545).
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

namespace Quest.Katalam;

public sealed class _20084NotDesertNorDanger : QuestHandlerBase
{
    private const int QuestIdConst = 20084;
    private static readonly int[] Mobs = { 230405, 230406 };

    private readonly IItemDao _itemDao;

    public _20084NotDesertNorDanger(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(182215234, QuestId);
        RegisterOnAtDistance(engine, 206285);
        RegisterOnAtDistance(engine, 231222);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npcId in new[] { 800553, 800554, 801153, 800555, 800565, 701538, 800545 })
            engine.RegisterQuestNpc(npcId).OnTalk.Add(QuestId);
        foreach (int mob in Mobs)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 20083, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is not null && entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == 800553) // Tolkin
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 6) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                }
                // Java switch fallthrough: QUEST_SELECT (no var match) falls into SETPRO1
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                if (dialog == DialogAction.SETPRO4)
                    return await DefaultCloseDialogAsync(env, conn, 6, 7, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 2, false, 10000, 10001, ct);
                return false;
            }
            if (targetId == 800554) // Jeminu
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 7)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                // Java switch fallthrough: QUEST_SELECT (var!=7) falls into SETPRO5
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO5)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, 182215234, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 7, 8, ct);
                }
                return false;
            }
            if (targetId == 801153) // Theodrik
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 9)
                    return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                // Java switch fallthrough: QUEST_SELECT (var!=9) falls into SETPRO7
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO7)
                    return await DefaultCloseDialogAsync(env, conn, 9, 10, ct);
                return false;
            }
            if (targetId == 800555) // Body of a Beritra Soldier
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 10)
                    return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                // Java switch fallthrough: QUEST_SELECT (var!=10) falls into SETPRO8
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO8)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, 182215238, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 10, 11, ct);
                }
                return false;
            }
            if (targetId == 800565) // Otro
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 12)
                    return await SendQuestDialogAsync(conn, targetObjId, 4080, ct);
                // Java switch fallthrough: QUEST_SELECT (var!=12) falls into SETPRO10
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO10)
                    return await DefaultCloseDialogAsync(env, conn, 12, 13, ct);
                return false;
            }
            if (targetId == 701538) // Broken Sword
            {
                if (dialog == DialogAction.USE_OBJECT && var == 13)
                    return await SendQuestDialogAsync(conn, targetObjId, 4082, ct);
                // Java switch fallthrough: USE_OBJECT (var!=13) falls into SETPRO11
                if (dialog == DialogAction.USE_OBJECT || dialog == DialogAction.SETPRO11)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, 182215239, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 13, 14, reward: true, sameNpc: false, ct);
                }
                return false;
            }
        }
        else if (entry is not null && entry.Status == QuestStatus.REWARD && targetId == 800545) // Niel
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, 182215239, 1, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, 182215238, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, Mobs, 2, 6, ct);

    // Java onItemUseEvent: var 0 == 8 -> advance to 9 (no reward) and consume the item.
    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != 182215234) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.GetVar(0) != 8) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 9, toReward: false, ct);
        await RemoveQuestItemAsync(player, conn, _itemDao, 182215234, 1, ct);
        return true;
    }

    public override async ValueTask<bool> OnAtDistanceAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) == 11)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 12, toReward: false, ct);
            return true;
        }
        return false;
    }
}
