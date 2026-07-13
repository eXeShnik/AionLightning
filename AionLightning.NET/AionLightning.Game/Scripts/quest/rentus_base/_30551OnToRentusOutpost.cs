// Port of Java data/scripts/system/handlers/quest/rentus_base/_30551OnToRentusOutpost.java (Ritsu).
// Start at Oreitia (799544); entering RENTUS_BASE_300280000 flips the quest straight to REWARD
// (Java changeQuestStep(env, 0, 1, true) — reward=true ignores nextStep, so var0 stays 0); turn in
// at Ariana (799666). The START-status SET_SUCCEED branch on 799666 is dead in normal flow (the
// zone-enter already moved the quest to REWARD) but is preserved to match Java.
// note: Java's canRepeat() re-entry is approximated as "no active entry".
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

namespace Quest.RentusBase;

public sealed class _30551OnToRentusOutpost : QuestHandlerBase
{
    private const int QuestIdConst = 30551;
    private const int OreitiaNpc   = 799544;
    private const int ArianaNpc    = 799666;
    private const string RentusBaseZone = "RENTUS_BASE_300280000";

    public _30551OnToRentusOutpost(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(OreitiaNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(OreitiaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ArianaNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, RentusBaseZone);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null) // Java: qs == null || NONE || canRepeat()
        {
            if (targetId == OreitiaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == ArianaNpc)
            {
                if (dialog == DialogAction.SET_SUCCEED)
                    return await DefaultCloseDialogAsync(env, conn, 1, 1, reward: true, sameNpc: false, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == ArianaNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != RentusBaseZone) return false;
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is not null && entry.Status == QuestStatus.START)
        {
            if (entry.GetVar(0) == 0)
            {
                // Java changeQuestStep(env, 0, 1, true): reward=true flips to REWARD and ignores nextStep (var0 stays 0).
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
        }
        return false;
    }
}
