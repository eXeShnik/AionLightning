// Port of Java data/scripts/system/handlers/quest/rider_quests/_24030ShowdownWithDestiny.java (pralinka).
// Altgard/Morheim chain: Rhotan (204206, var0 0->1), Ampeleus (204207, 1->2), Munin (203550, 2->3;
// CHECK collects item 182215392 3->4; 4->5; SET_SUCCEED 8->REWARD), USE Wind Serpent (700551,
// var0==5) enters instance world 320140000, Kaidan (205020, var0==5) flight-teleports and after ~43s
// advances var0 5->6; inside, kill the mob pack (798342-798346) counting var1 0->49, at var1==49
// spawn 798346 and advance var0 6->7, final kill of 798346 (var0 7->8). Turn in at 204052 in REWARD.
// Dying or leaving 320140000 while var0 in (5,8) reverts to var0 5.
// Instance entry uses EnterInstanceAsync; in-instance spawn uses SpawnQuestNpc; player-death revert
// uses OnDieAsync.
// Java quirk preserved: NPC 205020 (the flight NPC) is NOT in the register()'d talk list, so its
// dialog branch is unreachable in the shipped Java source; it is ported verbatim (dead branch) for
// structural fidelity and only the five listed NPCs are registered.
// Skips vs Java: (1) Kaidan's flight visuals (SkillEngine effect 1910, FLIGHT_TELEPORT state,
// START_FLYTELEPORT emotion) dropped — no flight-teleport infra; the 43s delay + var0 5->6 preserved
// via a scheduled task. (2) the var0==7 onKill TeleportService2 relocation to Altgard (220010000)
// dropped — non-entry relocation, the var0 7->8 transition is kept. (3) QUEST_FAILED_$1 system
// message on the die/leave reset dropped — cosmetic notice.
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

namespace Quest.RiderQuests;

public sealed class _24030ShowdownWithDestiny : QuestHandlerBase
{
    private const int QuestIdConst = 24030;
    private const int RhotanNpc    = 204206;
    private const int AmpeleusNpc  = 204207;
    private const int MuninNpc     = 203550;
    private const int SerpentObj   = 700551;
    private const int KaidanNpc    = 205020;
    private const int TurnInNpc    = 204052;
    private const int FinalMob     = 798346;
    private const int CollectItem  = 182215392;
    private const int InstanceWorld = 320140000;

    private static readonly int[] _mobs = [798342, 798343, 798344, 798345, 798346];

    private readonly IItemDao _itemDao;

    public _24030ShowdownWithDestiny(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        RegisterOnDie(engine);
        engine.RegisterOnEnterWorld(QuestId);
        foreach (int npc in new[] { RhotanNpc, AmpeleusNpc, MuninNpc, SerpentObj, TurnInNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        foreach (int mob in _mobs) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);
        int var         = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == RhotanNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == AmpeleusNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == MuninNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    if (var == 8) return await SendQuestDialogAsync(conn, targetObjId, 3740, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO3) return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM && var == 3)
                {
                    var item = player.Inventory.FindByItemId(CollectItem);
                    if (item is not null && item.Count >= 1)
                    {
                        await RemoveQuestItemAsync(player, conn, _itemDao, CollectItem, 1, ct);
                        await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: false, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 10000, ct);
                    }
                    return await SendQuestDialogAsync(conn, targetObjId, 10001, ct);
                }
                if (dialog == DialogAction.SETPRO5) return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                if (dialog == DialogAction.SET_SUCCEED) return await DefaultCloseDialogAsync(env, conn, 8, 8, reward: true, sameNpc: false, ct);
                return false;
            }
            if (targetId == SerpentObj)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 5)
                    return await EnterInstanceAsync(player, conn, InstanceWorld, 52f, 174f, 229f, 0, ct);
                return false;
            }
            if (targetId == KaidanNpc) // Java quirk: unregistered, unreachable — ported for fidelity
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 5) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (dialog == DialogAction.SETPRO6 && var == 5)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, CollectItem, 1, ct);
                    // note: flight visuals (SkillEngine effect 1910, FLIGHT_TELEPORT state, START_FLYTELEPORT emotion) dropped — no flight-teleport infra; 43s delay + var0 5->6 preserved
                    var scheduled = entry;
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(43000);
                        try { await ChangeQuestStepAsync(conn, scheduled, 0, 6, toReward: false, CancellationToken.None); } catch { }
                    });
                    return true;
                }
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == TurnInNpc) return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var == 6)
        {
            int var1 = entry.GetVar(1);
            if (var1 >= 0 && var1 < 49)
            {
                if (System.Array.IndexOf(_mobs, env.TargetId) >= 0)
                {
                    entry.SetVar(1, var1 + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return true;
                }
                return false;
            }
            if (var1 == 49)
            {
                SpawnQuestNpc(InstanceWorld, player.Position.InstanceId, FinalMob, 240f, 257f, 208.53946f, 68);
                entry.SetVar(0, 7);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
        }
        else if (var == 7)
        {
            // note: TeleportService2 relocation to Altgard (220010000) dropped — non-entry relocation; var0 7->8 kept
            return await DefaultOnKillEventAsync(env, conn, FinalMob, 7, 8, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var > 5 && var < 8)
        {
            // note: QUEST_FAILED_$1 system message dropped — cosmetic notice
            await ChangeQuestStepAsync(conn, entry, 0, 5, toReward: false, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (player.Position.WorldId == InstanceWorld) return false;
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var > 5 && var < 8)
        {
            // note: QUEST_FAILED_$1 system message dropped — cosmetic notice
            await ChangeQuestStepAsync(conn, entry, 0, 5, toReward: false, ct);
            return true;
        }
        return false;
    }
}
