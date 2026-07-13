// Port of Java data/scripts/system/handlers/quest/tiamaranta/_41527ANiceHealingBath.java (Cheatkiller).
// Talk to 205895 to start; talk to 205964 to advance var 0->1 (SETPRO1); kill 219206 while at var 1
// advances 1->2; entering STEAM_SPRINGS_600030000 at var 2 flips to REWARD; turn in at 205964.
// Skip vs Java: the two SkillEngine.applyEffectDirectly calls (a heal buff 20437 granted on the kill,
// and effect 8756 on entering the springs) are cosmetic buffs that do not gate any state transition -
// no direct effect-application API is wired into quest scripts, so they are dropped; every step change
// still fires unconditionally, exactly as in Java.
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

namespace Quest.Tiamaranta;

public sealed class _41527ANiceHealingBath : QuestHandlerBase
{
    private const int QuestIdConst = 41527;
    private const int StartNpc     = 205895;
    private const int TalkNpc      = 205964;
    private const int KillNpc      = 219206;
    private const string SteamSpringsZone = "STEAM_SPRINGS_600030000";

    public _41527ANiceHealingBath(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(TalkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KillNpc).OnKill.Add(QuestId);
        RegisterOnEnterZone(engine, SteamSpringsZone);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == TalkNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TalkNpc)
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
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;
        // note: Java applies heal buff 20437 here (cosmetic) - no effect-application API in scripts, dropped.
        return await DefaultOnKillEventAsync(env, conn, KillNpc, 1, 2, ct);
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != SteamSpringsZone) return false;
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 2) return false;
        // note: Java applies effect 8756 here (cosmetic) - dropped as above.
        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: true, ct);
        return true;
    }
}
