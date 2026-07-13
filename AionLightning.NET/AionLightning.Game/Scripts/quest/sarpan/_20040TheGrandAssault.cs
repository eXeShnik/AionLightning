// Port of Java data/scripts/system/handlers/quest/sarpan/_20040TheGrandAssault.java (zhkchi).
// Asmodian twin of sarpan/_10040AssaultOnTiamaranta (Sadonic, world 300410000). Start at 799225
// (var 0->1) -> 800085 SETPRO2 enters the instance (791,745,997, heading 2) and advances (1->2) ->
// 799722 SETPRO3 (2->3) -> 730528 USE_OBJECT relocates deeper (dropped) -> attack the boss 205812 at
// var==3: after an 18s scripted sequence the var flips 3->4->11 -> 800280 SET_SUCCEED flips to REWARD
// (relocation to reward world 600020000 dropped) -> turn in at 205617 in REWARD. Leaving the instance
// world (onEnterWorld/onDie) or leaving the SADONICS_CAPTAINS_CABIN/SADONICS_DECK zone regions while
// in progress reverts to var 1.
// Unblocked by RegisterOnLeaveZone/OnLeaveZoneAsync (Java onLeaveZoneEvent) + EnterInstanceAsync
// (Java InstanceService triad); level-up fans out zone-mission-end pokes to 20050-20053 via the
// captured engine (Java QuestEngine.onEnterZoneMissionEnd), mirroring quest/brusthonin/_2091MeettheReapers.
// Skips vs Java (all cosmetic, state transitions preserved): SM_PLAY_MOVIE(1,19) instance-entry
// cutscene; the two TeleportService2 relocations (730528 in-instance hop, 800280 reward-world hop);
// the boss-attack effect burst (skill 20412 on player + every non-boss instance NPC), the shout
// broadcasts (1111387-1111392) and the 18s NPC despawn — no skill-effect / instance-NPC-enumeration /
// despawn infra; the 18s var 4->11 transition is kept on the same schedule. QUEST_FAILED_$1 notices dropped.
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

namespace Quest.Sarpan;

public sealed class _20040TheGrandAssault : QuestHandlerBase
{
    private const int QuestIdConst = 20040;
    private const int StartNpc      = 799225;
    private const int Npc800085     = 800085;
    private const int Npc799722     = 799722;
    private const int Npc730528     = 730528;
    private const int Npc800280     = 800280;
    private const int TurnInNpc     = 205617;
    private const int BossMob       = 205812;
    private const int InstanceWorld = 300410000;
    private const int RewardWorld   = 600020000;
    private const string ZoneCabin  = "SADONICS_CAPTAINS_CABIN_300410000";
    private const string ZoneDeck   = "SADONICS_DECK_300410000";

    private static readonly int[] _zoneMissions = [20050, 20051, 20052, 20053];

    private QuestEngine? _engine;

    public _20040TheGrandAssault(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        _engine = engine;
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Npc800085).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc799722).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc800280).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc730528).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BossMob).OnAttack.Add(QuestId);
        RegisterOnLeaveZone(engine, ZoneCabin);
        RegisterOnLeaveZone(engine, ZoneDeck);
    }

    public override async ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        if (await DefaultOnLvlUpEventAsync(env, conn, ct))
        {
            if (_engine is not null)
                foreach (int id in _zoneMissions)
                    await _engine.OnZoneMissionEndAsync(new QuestEnv(env.Target, env.Player, id, env.DialogId), conn, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        if (entry.Status == QuestStatus.START)
        {
            if (player.Position.WorldId != InstanceWorld && entry.GetVar(0) > 1)
            {
                entry.SetVar(0, 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                // note: QUEST_FAILED_$1 system message dropped — cosmetic notice.
                return true;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (player.Position.WorldId != RewardWorld)
            {
                entry.Status = QuestStatus.START;
                entry.SetVar(0, 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                // note: QUEST_FAILED_$1 system message dropped — cosmetic notice.
                return true;
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == StartNpc && var == 0)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (targetId == Npc800085 && var == 1)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    await EnterInstanceAsync(player, conn, InstanceWorld, 791f, 745f, 997f, 2, ct);
                    await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
                    // note: Java plays SM_PLAY_MOVIE(1, 19) instance-entry cutscene here — cosmetic movie broadcast, dropped.
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (targetId == Npc799722 && var == 2)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SELECT_ACTION_1694)
                    return await SendQuestDialogAsync(conn, targetObjId, 1694, ct);
                if (dialog == DialogAction.SETPRO3)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (targetId == Npc730528 && var == 3)
            {
                if (dialog == DialogAction.USE_OBJECT)
                {
                    // note: Java relocates within the instance (300410000, 774,743,997) — non-entry relocation, dropped; return true kept.
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (targetId == Npc800280)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (dialog == DialogAction.SELECT_ACTION_2717)
                    return await SendQuestDialogAsync(conn, targetObjId, 2717, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    // note: Java teleports to reward world 600020000 (1511,1559,1359, heading 70, BEAM) here — non-entry relocation, dropped.
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == TurnInNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnAttackAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) != 3 || env.TargetId != BossMob) return false;

        // Java sets var 4 in memory (no updateQuestStatus yet), applies skill 20412 to the player and
        // every non-boss instance NPC (18s), broadcasts shouts 1111387-1111392, then after 18s despawns
        // those NPCs and flips var 4->11 (updateQuestStatus). The effect burst / instance-NPC enumeration
        // / despawn are unported cosmetics; only the var transitions are kept, on the same 18s schedule.
        entry.SetVar(0, 4);
        var scheduled = entry;
        _ = Task.Run(async () =>
        {
            await Task.Delay(18000);
            try
            {
                scheduled.SetVar(0, 11);
                await UpdateQuestStatusAsync(conn, scheduled, CancellationToken.None);
            }
            catch { }
        });
        return false;
    }

    public override async ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (entry.GetVar(0) > 1)
        {
            entry.SetVar(0, 1);
            await UpdateQuestStatusAsync(conn, entry, ct);
            // note: QUEST_FAILED_$1 system message dropped — cosmetic notice.
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnLeaveZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (zoneName == ZoneCabin)
        {
            if (entry.GetVar(0) < 3)
            {
                entry.SetVar(0, 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
        }
        else if (zoneName == ZoneDeck)
        {
            entry.SetVar(0, 1);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }
}
