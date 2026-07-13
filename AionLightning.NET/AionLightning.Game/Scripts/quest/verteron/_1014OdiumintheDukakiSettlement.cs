// Port of Java data/scripts/system/handlers/quest/verteron/_1014OdiumintheDukakiSettlement.java
// (Rhys2002). Talk to Krotan (203129), kill Dukaki mobs (210145/210146) up to var 10, brew the
// Odium in the refining cauldron (700090/item 182200012), turn in at Spatalos (203098).
// Zone-mission-end/level-up gated on quest 1130.
// Java bug: case SELECT_ACTION_1013 falls through (missing break) into SETPRO1's and then
// SETPRO2's var-guarded bodies when var != 0 — SETPRO2's body could misfire a var10->11 transition
// for an unrelated dialog id. Ported here as independent per-case branches instead.
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

namespace Quest.Verteron;

public sealed class _1014OdiumintheDukakiSettlement : QuestHandlerBase
{
    private const int QuestIdConst = 1014;
    private const int KrotanNpc    = 203129;
    private const int GuideNpc     = 730020;
    private const int SpatalosNpc  = 203098;
    private const int CauldronNpc  = 700090;
    private const int CauldronItem = 182200012;
    private const int PreparedItem = 182200011;
    private const int SpawnedMob   = 210739;
    private const string CauldronZone = "ODIUM_REFINING_CAULDRON_210030000";

    private static readonly int[] _npcs = [KrotanNpc, GuideNpc, SpatalosNpc, CauldronNpc];
    private static readonly int[] _mobs = [210145, 210146, 210174, SpawnedMob];

    private readonly IItemDao _itemDao;

    public _1014OdiumintheDukakiSettlement(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestItem(CauldronItem, QuestId);
        foreach (int mob in _mobs)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
        foreach (int npc in _npcs)
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1130, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        if (entry.Status == QuestStatus.REWARD && env.TargetId == SpatalosNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (env.TargetId == KrotanNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 0:
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                case DialogAction.QUEST_SELECT when var == 10:
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                case DialogAction.QUEST_SELECT when var == 14:
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                case DialogAction.SELECT_ACTION_1013 when var == 0:
                    await PlayQuestMovieAsync(conn, player, 26, ct);
                    return false;
                case DialogAction.SETPRO1 when var == 0:
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                case DialogAction.SETPRO2 when var == 10:
                    return await DefaultCloseDialogAsync(env, conn, 10, 11, ct);
                case DialogAction.SETPRO3 when var == 14:
                    return await DefaultCloseDialogAsync(env, conn, 14, 14, reward: true, sameNpc: false, ct);
                default:
                    return false;
            }
        }

        if (env.TargetId == GuideNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 1:
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                case DialogAction.SETPRO2 when var == 1:
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                default:
                    return false;
            }
        }

        if (env.TargetId == CauldronNpc && var == 11 && dialog == DialogAction.USE_OBJECT)
        {
            if ((player.Inventory.FindByItemId(PreparedItem)?.Count ?? 0) == 0) return false;
            SpawnQuestNpc(210030000, 1, SpawnedMob, 757.7f, 2477.2f, 217.4f, 0);
            return false;
        }

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.TargetId != 210145 && env.TargetId != 210146) return false;
        if (entry.GetVar(0) >= 10) return false;

        await ChangeQuestStepAsync(conn, entry, 0, entry.GetVar(0) + 1, toReward: false, ct);
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != CauldronItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 11) return false;
        if (!player.CurrentZones.Contains(CauldronZone)) return false;

        // Java's useQuestItem-style 3s SM_ITEM_USAGE_ANIMATION cast delay is collapsed to an
        // immediate effect (matches the established simplification, see _1197KrallBook/_1182 ports).
        await PlayQuestMovieAsync(conn, player, 172, ct);
        await RemoveQuestItemAsync(player, conn, _itemDao, CauldronItem, 1, ct);
        await RemoveQuestItemAsync(player, conn, _itemDao, PreparedItem, 1, ct);
        await ChangeQuestStepAsync(conn, entry, 0, 14, toReward: false, ct);
        return true;
    }
}
