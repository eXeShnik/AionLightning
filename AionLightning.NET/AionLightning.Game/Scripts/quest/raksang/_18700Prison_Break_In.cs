// Port of Java data/scripts/system/handlers/quest/raksang/_18700_Prison_Break_In.java (Cheatkiller).
// Elyos raid quest inside the Raksang dungeon. Start at npc 799532; talk to 799439 (SETPRO1 var0
// 0->1), 799438 (SETPRO2 var0 1->2), then a two-stage kill chain: destroy the 4 generators
// (730453/54/55/56, var0 2->3), kill 217392 (3->4), talk to 799429 with movie 455 (SETPRO5 4->5),
// kill the two bosses 217425/217451 (5->6), re-enter zone RAKSANG_DUNGEON_CHASM_300310000 (6->7),
// kill 217764 (7->8), kill 217647 (8->9), then talk to 799439 (SET_SUCCEED) to flip REWARD; turn
// in back at 799532. Npc 730468 is registered for talk but unused, preserved from Java.
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

namespace Quest.Raksang;

public sealed class _18700Prison_Break_In : QuestHandlerBase
{
    private const int QuestIdConst = 18700;
    private const int StartNpc     = 799532;
    private const int Npc799439    = 799439;
    private const int Npc799429    = 799429;
    private const int Npc799438    = 799438;
    private const int Npc730468    = 730468;
    private const string EnterZoneName = "RAKSANG_DUNGEON_CHASM_300310000";

    private static readonly int[] Mobs = { 730453, 730454, 730455, 730456, 217392, 217425, 217451, 217764, 217647 };

    public _18700Prison_Break_In(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc799439).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc799429).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc799438).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc730468).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, EnterZoneName);
        foreach (int mob in Mobs)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
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
            if (entry.Status == QuestStatus.REWARD)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }

        if (entry is null) return false;

        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == Npc799439)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
            else if (targetId == Npc799429)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO5)
                {
                    await PlayQuestMovieAsync(conn, player, 455, ct);
                    return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                }
            }
        }
        else if (entry.Status != QuestStatus.START)
        {
            return false;
        }

        if (targetId == Npc799438)
        {
            // Java switch fallthrough: QUEST_SELECT with var==1 shows page 1352; QUEST_SELECT with
            // any other var falls through into the SETPRO2 case, so both QUEST_SELECT (var!=1) and
            // SETPRO2 advance var0 1->2.
            if (dialog == DialogAction.QUEST_SELECT && var == 1)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO2)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
        }

        if (entry.Status == QuestStatus.START && var == 9)
        {
            if (targetId == Npc799439)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4080, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry    = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var == 2)
        {
            await CheckAndUpdateVarGenAsync(conn, entry, targetId, ct);
            return false;
        }
        if (var == 3)
            return await DefaultOnKillEventAsync(env, conn, 217392, 3, 4, ct);
        if (var == 5)
        {
            await CheckAndUpdateVarBossesAsync(conn, entry, targetId, ct);
            return false;
        }
        if (var == 7)
            return await DefaultOnKillEventAsync(env, conn, 217764, 7, 8, ct);
        if (var == 8)
            return await DefaultOnKillEventAsync(env, conn, 217647, 8, 9, ct);
        return false;
    }

    private async ValueTask CheckAndUpdateVarGenAsync(GsClientConnection conn, QuestEntry entry, int targetId, CancellationToken ct)
    {
        switch (targetId)
        {
            case 730453:
                entry.SetVar(1, 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await IsAllKilledGeneratorAsync(conn, entry, ct);
                break;
            case 730454:
                entry.SetVar(2, 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await IsAllKilledGeneratorAsync(conn, entry, ct);
                break;
            case 730455:
                entry.SetVar(3, 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await IsAllKilledGeneratorAsync(conn, entry, ct);
                break;
            case 730456:
                entry.SetVar(4, 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await IsAllKilledGeneratorAsync(conn, entry, ct);
                break;
        }
    }

    private async ValueTask IsAllKilledGeneratorAsync(GsClientConnection conn, QuestEntry entry, CancellationToken ct)
    {
        if (entry.GetVar(1) == 1 && entry.GetVar(2) == 1 && entry.GetVar(3) == 1 && entry.GetVar(4) == 1)
        {
            entry.SetVar(1, 0);
            entry.SetVar(2, 0);
            entry.SetVar(3, 0);
            entry.SetVar(4, 0);
            await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
        }
    }

    private async ValueTask CheckAndUpdateVarBossesAsync(GsClientConnection conn, QuestEntry entry, int targetId, CancellationToken ct)
    {
        switch (targetId)
        {
            case 217425:
                entry.SetVar(1, 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await IsAllKilledBossesAsync(conn, entry, ct);
                break;
            case 217451:
                entry.SetVar(2, 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await IsAllKilledBossesAsync(conn, entry, ct);
                break;
        }
    }

    private async ValueTask IsAllKilledBossesAsync(GsClientConnection conn, QuestEntry entry, CancellationToken ct)
    {
        if (entry.GetVar(1) == 1 && entry.GetVar(2) == 1)
        {
            entry.SetVar(1, 0);
            entry.SetVar(2, 0);
            await ChangeQuestStepAsync(conn, entry, 0, 6, toReward: false, ct);
        }
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != EnterZoneName) return false;

        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;

        if (entry.GetVar(0) == 6)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 7, toReward: false, ct);
            return true;
        }
        return false;
    }
}
