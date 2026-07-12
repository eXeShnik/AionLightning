// Port of Java data/scripts/system/handlers/quest/danaria/_23359RolfRequest.java (Romanz).
// Auto-starts on entering Danaria's PRADETH_MEADOWS_600060000 region (registerOnEnterZone -> QuestService.startQuest);
// kill one opposing-race player within level range [victimLevel-5, victimLevel+9] anywhere in
// Danaria (world 600060000) to complete (Java's onKillInWorldEvent additionally required
// player.isInsideZone(PRADETH_MEADOWS_600060000) — no zone-shape infra in this port, so the trigger is broadened to
// "anywhere in the world" like every other kill_in_world port here, e.g. sarpan's
// _11522FurbackHunting); turn in at RolfRequest (801055). qs.canRepeat() (daily-reset re-entry) isn't
// ported, matching every other kill_in_world quest already in this codebase.
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

namespace Quest.Danaria;

public sealed class _23359RolfRequest : QuestHandlerBase
{
    private const int QuestIdConst   = 23359;
    private const int TurnInNpc      = 801055;
    private const int DanariaWorldId = 600060000;
    private const string EnterZoneName = "PRADETH_MEADOWS_600060000";

    public _23359RolfRequest(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, EnterZoneName);
        engine.RegisterKillInWorld(DanariaWorldId, QuestId);
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != EnterZoneName) return false;
        if (!await StartMissionAsync(conn, env.Player, QuestStatus.START, ct)) return false;
        await CloseDialogWindowAsync(conn, 0, ct);
        return true;
    }

    public override async ValueTask<bool> OnPlayerKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.Target is not Player victim) return false;

        int killerLevel = env.Player.Level;
        int victimLevel = victim.Level;
        if (killerLevel < victimLevel - 5 || killerLevel > victimLevel + 9) return false;

        await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.REWARD) return false;
        if (env.TargetId != TurnInNpc) return false;

        int targetObjId = env.Target?.ObjectId ?? 0;
        if (DialogActionLookup.FromId(env.DialogId) == DialogAction.USE_OBJECT)
            return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);

        return await SendQuestEndDialogAsync(env, conn, ct);
    }
}
