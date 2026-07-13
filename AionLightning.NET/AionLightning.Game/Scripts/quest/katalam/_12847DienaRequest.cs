// Port of Java data/scripts/system/handlers/quest/katalam/_12847DienaRequest.java (Romanz).
// Auto-starts on entering Katalam's PRIMEVAL_RUINS_600050000 region (registerOnEnterZone ->
// QuestService.startQuest); kill one opposing-race player within level range [victimLevel-5,
// victimLevel+9] anywhere in Katalam (world 600050000) to complete (Java's onKillInWorldEvent
// additionally required player.isInsideZone(PRIMEVAL_RUINS_600050000) — no zone-shape infra in this
// port, so the trigger is broadened to "anywhere in the world", matching every other kill_in_world
// port here, e.g. danaria's _23350HervoRequest); turn in at DienaRequest (801233). qs.canRepeat()
// (daily-reset re-entry) isn't ported, matching every other kill_in_world quest already in this
// codebase.
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

public sealed class _12847DienaRequest : QuestHandlerBase
{
    private const int QuestIdConst   = 12847;
    private const int TurnInNpc      = 801233;
    private const int KatalamWorldId = 600050000;
    private const string EnterZoneName = "PRIMEVAL_RUINS_600050000";

    public _12847DienaRequest(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, EnterZoneName);
        engine.RegisterKillInWorld(KatalamWorldId, QuestId);
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
