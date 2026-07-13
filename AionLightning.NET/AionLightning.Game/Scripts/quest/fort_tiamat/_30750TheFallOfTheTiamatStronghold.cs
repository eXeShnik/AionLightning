// Port of Java data/scripts/system/handlers/quest/fort_tiamat/_30750TheFallOfTheTiamatStronghold.java (Cheatkiller).
// Asmodian counterpart of _30700RaceForTheRelics with an identical flow; only the start/turn-in NPC
// differs (205864). Start at 205864 (page 4762); on entering the dredgion world (300510000) var 0 -> 1,
// kill 219400 1->2, entering DREDGION_CONTROL_CENTER_2 zone 2->3, talk to 800368 (SETPRO4) 3->4, kill
// 219406 4->5, use 800338 (SET_SUCCEED) 5->6 REWARD; turn in at 205864. Leaving the dredgion world with
// var >= 1 resets progress back to var 0 = 1.
// note: Java's onEnterWorld reset also shows SM_SYSTEM_MESSAGE(QUEST_FAILED_$1, questName); no matching
// factory is exposed in this port (see sarpan/_21506This_End_Up), so the toast is dropped while the
// state reset is preserved.
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

namespace Quest.FortTiamat;

public sealed class _30750TheFallOfTheTiamatStronghold : QuestHandlerBase
{
    private const int QuestIdConst    = 30750;
    private const int StartNpc        = 205864;
    private const int TalkNpc         = 800368;
    private const int UseNpc          = 800338;
    private const int Mob1            = 219400;
    private const int Mob2            = 219406;
    private const int DredgionWorldId = 300510000;
    private const string EnterZoneName = "DREDGION_CONTROL_CENTER_2_300510000";

    public _30750TheFallOfTheTiamatStronghold(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TalkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(UseNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Mob1).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Mob2).OnKill.Add(QuestId);
        RegisterOnEnterZone(engine, EnterZoneName);
        engine.RegisterOnEnterWorld(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == TalkNpc)
            {
                // Java switch fallthrough: QUEST_SELECT returns only when var == 3; otherwise falls to SETPRO4.
                if (dialog == DialogAction.QUEST_SELECT && var == 3)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO4)
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                return false;
            }
            if (targetId == UseNpc)
            {
                // Java switch fallthrough: USE_OBJECT returns only when var == 5; otherwise falls to SET_SUCCEED.
                if (dialog == DialogAction.USE_OBJECT && var == 5)
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                    return await DefaultCloseDialogAsync(env, conn, 5, 6, reward: true, sameNpc: false, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var == 1)
            return await DefaultOnKillEventAsync(env, conn, Mob1, 1, 2, ct);
        if (var == 4)
            return await DefaultOnKillEventAsync(env, conn, Mob2, 4, 5, ct);
        return false;
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != EnterZoneName) return false;
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) != 2) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (player.Position.WorldId != DredgionWorldId)
        {
            if (var >= 1)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                // note: Java also sends SM_SYSTEM_MESSAGE(QUEST_FAILED_$1) here — dropped, see header.
                return true;
            }
        }
        else if (var == 0)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
            return true;
        }
        return false;
    }
}
