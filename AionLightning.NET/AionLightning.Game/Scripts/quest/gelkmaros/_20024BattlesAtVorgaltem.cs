// Port of Java data/scripts/system/handlers/quest/gelkmaros/_20024BattlesAtVorgaltem.java (Nephis,
// reworked Gigi/vlog). Valetta (799226, var0->1) -> Ankea (799308: var1->2, var3->4) -> Vegrim
// (799298: var4->5, CHECK_USER_HAS_QUEST_ITEM var5->6, var7->8 gives 182207615, var9->10 gives
// 182207616, SET_SUCCEED var11->reward; SELECT_ACTION_1609 plays movie 554 at var11) -> Fjoersvith
// (798713, var6->7) -> Drana Control Tower (700707, uses+removes 182207616, var10->11) -> using
// item 182207615 inside DF4_ITEMUSEAREA_Q20024 (var8->9). Kill tracking at var0==2: 2 Naduka
// Wardrakes (216034/216033, var1 0->4 then 4->5 once var2==10) and 10 Vorgaltem balaurs
// (216448/216450/216451/216108/216449/216107/216112/216109/216104/216101, var2 0->9 then 9->10
// once var1==5) — either counter reaching its cap while the other is already done flips var0
// straight to 3. NPC 700811 is registered for OnKill parity (Java's mobs array includes it) but is
// never referenced in onKillEvent's switch — a dead registration in the original.
// Caveat: onLvlUpEvent gates this quest behind BOTH 20022 and 20023 being COMPLETE; 20023
// (_20023KumbandasWhereabouts) is not ported in this batch (InstanceService-gated), so this
// handler is fully functional but currently unreachable via the level-up path until 20023 exists.
using System.Collections.Generic;
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

public sealed class _20024BattlesAtVorgaltem : QuestHandlerBase
{
    private const int QuestIdConst    = 20024;
    private const int ValettaNpc      = 799226;
    private const int AnkeaNpc        = 799308;
    private const int VegrimNpc       = 799298;
    private const int FjoersvithNpc   = 798713;
    private const int ControlTowerObj = 700707;
    private const int NadukaA         = 216034;
    private const int NadukaB         = 216033;
    private const int ArmorItem       = 182207615;
    private const int TowerKeyItem    = 182207616;
    private const string ItemUseZone  = "DF4_ITEMUSEAREA_Q20024";

    private static readonly int[] _talkNpcs = [ValettaNpc, AnkeaNpc, VegrimNpc, FjoersvithNpc, ControlTowerObj];
    private static readonly int[] _nadukas   = [NadukaA, NadukaB];
    private static readonly int[] _balaurs   = [216448, 216450, 216451, 216108, 216449, 216107, 216112, 216109, 216104, 216101];
    private static readonly int[] _allMobs   = [216104, 216101, 216109, 216112, 216107, 216033, 216034, 216448, 216450, 216451, 216108, 216449, 700811];

    private readonly IItemDao _itemDao;

    public _20024BattlesAtVorgaltem(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterQuestItem(ArmorItem, QuestId);
        foreach (int mob in _allMobs) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
        foreach (int npc in _talkNpcs) engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, (IReadOnlyCollection<int>)[20022, 20023], isZoneMission: true, ct);

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

            if (targetId == ValettaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }

            if (targetId == AnkeaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                if (dialog == DialogAction.SETPRO4)
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                return false;
            }

            if (targetId == VegrimNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 4)  return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    if (var == 5)  return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                    if (var == 7)  return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                    if (var == 9)  return await SendQuestDialogAsync(conn, targetObjId, 4080, ct);
                    if (var == 11) return await SendQuestDialogAsync(conn, targetObjId, 1608, ct);
                    return false;
                }
                if (dialog == DialogAction.SELECT_ACTION_1609)
                {
                    if (var != 11) return false;
                    await PlayQuestMovieAsync(conn, player, 554, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1609, ct);
                }
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 5, 6, false, 10000, 10001, ct);
                if (dialog == DialogAction.SETPRO8)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 7, 8, reward: false, sameNpc: false,
                        ArmorItem, 1, 0, 0, ct);
                if (dialog == DialogAction.SETPRO10)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 9, 10, reward: false, sameNpc: false,
                        TowerKeyItem, 1, 0, 0, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                    return await DefaultCloseDialogAsync(env, conn, 11, 11, reward: true, sameNpc: false, ct);
                if (dialog == DialogAction.FINISH_DIALOG)
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                return false;
            }

            if (targetId == FjoersvithNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 6)
                    return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                if (dialog == DialogAction.SETPRO7)
                    return await DefaultCloseDialogAsync(env, conn, 6, 7, ct);
                return false;
            }

            if (targetId == ControlTowerObj)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await UseQuestObjectAsync(env, conn, 10, 11, reward: false, varNum: 0,
                        addItemId: 0, addItemCount: 0, removeItemId: TowerKeyItem, removeItemCount: 1,
                        movieId: 0, dieObject: false, _itemDao, ct);
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

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) != 2) return false;

        int targetId = env.TargetId;
        int var1 = entry.GetVar(1);
        int var2 = entry.GetVar(2);

        if (targetId == NadukaA || targetId == NadukaB)
        {
            if (var1 < 4)
                return await BumpVarOnKillAsync(env, conn, _nadukas, 1, 0, 4, ct);
            if (var1 == 4)
            {
                if (var2 == 10)
                {
                    entry.SetVar(0, 3);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return true;
                }
                return await BumpVarOnKillAsync(env, conn, _nadukas, 1, 4, 5, ct);
            }
            return false;
        }

        if (System.Array.IndexOf(_balaurs, targetId) >= 0)
        {
            if (var2 < 9)
                return await BumpVarOnKillAsync(env, conn, _balaurs, 2, 0, 9, ct);
            if (var2 == 9)
            {
                if (var1 == 5)
                {
                    entry.SetVar(0, 3);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return true;
                }
                return await BumpVarOnKillAsync(env, conn, _balaurs, 2, 9, 10, ct);
            }
            return false;
        }

        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (!player.CurrentZones.Contains(ItemUseZone)) return false;

        var env = new QuestEnv(null, player, QuestId, 0);
        return await UseQuestObjectAsync(env, conn, 8, 9, reward: false, varNum: 0,
            addItemId: 0, addItemCount: 0, removeItemId: itemId, removeItemCount: 1,
            movieId: 0, dieObject: false, _itemDao, ct);
    }

    /// <summary>Java's defaultOnKillEvent(env, npcIds, startVar, endVar, varNum) overload — not
    /// ported to QuestHandlerBase since this is the only quest in this batch needing a non-zero
    /// var index span-bump; mirrors it exactly (status guard, membership check, half-open range).</summary>
    private async ValueTask<bool> BumpVarOnKillAsync(QuestEnv env, GsClientConnection conn,
        int[] npcIds, int varIdx, int startVar, int endVar, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (System.Array.IndexOf(npcIds, env.TargetId) < 0) return false;

        int var = entry.GetVar(varIdx);
        if (var < startVar || var >= endVar) return false;

        entry.SetVar(varIdx, var + 1);
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }
}
