// Port of Java data/scripts/system/handlers/quest/beshmundir/_30222AGuardsCurse.java (vlog).
// Talk to 798979 (Gelon) to start; kill 216239 (Ahbana the Wicked) to advance var 0->1; back at
// Gelon, SELECT_QUEST_REWARD (or QUEST_SELECT once var != 1 - Java's switch has no break between
// these cases, an intentional shortcut per the established precedent, see
// greater_stigma/_30217GroupStigmasScars) flips to REWARD; turn in at Gelon.
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

namespace Quest.Beshmundir;

public sealed class _30222AGuardsCurse : QuestHandlerBase
{
    private const int QuestIdConst = 30222;
    private const int GelonNpc     = 798979;
    private const int AhbanaNpc    = 216239;

    public _30222AGuardsCurse(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(GelonNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(GelonNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AhbanaNpc).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry       = env.Player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (env.TargetId != GelonNpc) return false;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (dialog == DialogAction.QUEST_SELECT && var == 1)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);

            if (dialog == DialogAction.SELECT_QUEST_REWARD || (dialog == DialogAction.QUEST_SELECT && var != 1))
            {
                await ChangeQuestStepAsync(conn, entry, 1, 1, toReward: true, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            }

            return false;
        }

        if (entry.Status == QuestStatus.REWARD) return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => await DefaultOnKillEventAsync(env, conn, AhbanaNpc, 0, 1, ct);
}
