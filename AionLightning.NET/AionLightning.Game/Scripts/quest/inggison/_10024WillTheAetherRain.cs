// Port of Java data/scripts/system/handlers/quest/inggison/_10024WillTheAetherRain.java (Nephis).
// Multi-npc chain: Pomponia (798970) drives most of the var progression and hands out the loop
// item 182206620 at var 7->8 and 11->8; Gelon (798979) advances var 1->2 and 3->4; the Parchment
// Map object (700605) advances var 2->3; Daphnis (203793) gate-checks the collected quest items at
// var 5->6 then advances 6->7; Donikia (799020) flips to reward at var 10; the Winged Naga
// (216498) advances var 9->10 both via dialog and via kill. Using the loop item while inside
// Hanarkand Prison's entrance zone at var 8 advances to var 9.
// Java's onDialogEvent nests each npc's dialog switch without a trailing break, so a dialog id that
// doesn't match any case for one npc falls through into the next npc's switch block; every
// fallthrough target re-validates its own step via defaultCloseDialog's internal var guard, so the
// net behavior is identical to the flat per-npc dialog checks below (harmless in practice since the
// client only ever sends dialog ids that pair with the npc actually being talked to).
// Skip vs Java: onLogOutEvent (would revert var 9 back to 11 if the player disconnects without the
// loop item) has no OnLogOut hook in this port - omitted, harmless to completion (see
// migration_plan.md, same simplification as eltnen._1033SatalocasHeart). SETPRO10's
// scheduleRespawn()/onDelete() on the Winged Naga is dropped - no NPC despawn/respawn API is
// exposed to hand-written quest scripts in this port (same simplification as sarpan._11512LoversLost).
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

namespace Quest.Inggison;

public sealed class _10024WillTheAetherRain : QuestHandlerBase
{
    private const int QuestIdConst = 10024;
    private const int PomponiaNpc    = 798970;
    private const int GelonNpc       = 798979;
    private const int ParchmentMapNpc = 700605;
    private const int DaphnisNpc     = 203793;
    private const int DonikiaNpc     = 799020;
    private const int WingedNagaNpc  = 216498;
    private const int LoopItem       = 182206620;
    private const int PrecedingQuest = 10022;
    private const string HanarkandZone = "HANARKAND_PRISON_ENTRANCE_210050000";

    private static readonly int[] TalkNpcs = { DonikiaNpc, ParchmentMapNpc, PomponiaNpc, GelonNpc, DaphnisNpc, WingedNagaNpc };

    private readonly IItemDao _itemDao;

    public _10024WillTheAetherRain(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestItem(LoopItem, QuestId);
        engine.RegisterQuestNpc(WingedNagaNpc).OnKill.Add(QuestId);
        foreach (int npcId in TalkNpcs)
            engine.RegisterQuestNpc(npcId).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);
        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == PomponiaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    if (var == 7) return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                    if (var == 11) return await SendQuestDialogAsync(conn, targetObjId, 3654, ct);
                }
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                if (dialog == DialogAction.SETPRO5)
                    return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                if (dialog == DialogAction.SETPRO8)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 7, 8, false, false, LoopItem, 1, 0, 0, ct);
                if (dialog == DialogAction.SETPRO11)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 11, 8, false, false, LoopItem, 1, 0, 0, ct);
                return false;
            }

            if (targetId == GelonNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                }
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                if (dialog == DialogAction.SETPRO4)
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                return false;
            }

            if (targetId == ParchmentMapNpc)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 2)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3)
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                return false;
            }

            if (targetId == DaphnisNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 5) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                    if (var == 6) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                }
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 5, 6, false, 10000, 10001, ct);
                if (dialog == DialogAction.SETPRO7)
                    return await DefaultCloseDialogAsync(env, conn, 6, 7, ct);
                if (dialog == DialogAction.FINISH_DIALOG)
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                return false;
            }

            if (targetId == DonikiaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 10)
                    return await SendQuestDialogAsync(conn, targetObjId, 1608, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                    return await DefaultCloseDialogAsync(env, conn, 10, 10, reward: true, sameNpc: false, ct);
                return false;
            }

            if (targetId == WingedNagaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 9)
                    return await SendQuestDialogAsync(conn, targetObjId, 4081, ct);
                if (dialog == DialogAction.SETPRO10)
                    return await DefaultCloseDialogAsync(env, conn, 9, 10, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == PomponiaNpc)
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
        if (itemId != LoopItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (!player.CurrentZones.Contains(HanarkandZone)) return false;
        if (entry.GetVar(0) != 8) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, LoopItem, 1, ct);
        await ChangeQuestStepAsync(conn, entry, 0, 9, toReward: false, ct);
        return true;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, WingedNagaNpc, startVar: 9, endVar: 10, ct);

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, PrecedingQuest, isZoneMission: true, ct);
}
