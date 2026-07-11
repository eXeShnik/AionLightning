// Port of Java data/scripts/system/handlers/quest/verteron/_1141BelbuasTreasure.java
// (Mr. Poke, Dune11, reworked vlog). Talk to Nola (730001) to start; use Belbua's Wine Barrel
// (700122) to flip straight to REWARD; turn in at the same barrel.
using System.Linq;
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

namespace Quest.Verteron;

public sealed class _1141BelbuasTreasure : QuestHandlerBase
{
    private const int QuestIdConst = 1141;
    private const int NolaNpc      = 730001;
    private const int WineBarrel   = 700122;

    public _1141BelbuasTreasure(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(NolaNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(NolaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(WineBarrel).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        var entry = player.Quests.Get(QuestId);

        if (entry is null)
        {
            if (targetId == NolaNpc)
            {
                if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, env.Target?.ObjectId ?? 0, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == WineBarrel)
        {
            int targetObjId = env.Target?.ObjectId ?? 0;
            var dialog = DialogActionLookup.FromId(env.DialogId);
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == WineBarrel)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
