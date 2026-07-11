// Port of Java data/scripts/system/handlers/quest/eltnen/_1376AMountaineOfTrouble.java (Atomics).
// Talk to Beramones (203947) to start; kill 2 mob ids x6 total then a bonus 7th kill flips to
// REWARD (Java's span + reward-flip defaultOnKillEvent combo); turn in at Agrips (203964).
using System.Collections.Generic;
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

namespace Quest.Eltnen;

public sealed class _1376AMountaineOfTrouble : QuestHandlerBase
{
    private const int QuestIdConst = 1376;
    private const int BeramonesNpc = 203947;
    private const int AgripsNpc    = 203964;

    private static readonly int[] _mobs = [210976, 210986];

    public _1376AMountaineOfTrouble(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(BeramonesNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(BeramonesNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AgripsNpc).OnTalk.Add(QuestId);
        foreach (int mob in _mobs) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == BeramonesNpc)
            {
                if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == AgripsNpc)
        {
            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => await DefaultOnKillEventAsync(env, conn, (IReadOnlyCollection<int>)_mobs, 0, 6, ct)
        || await DefaultOnKillEventAsync(env, conn, (IReadOnlyCollection<int>)_mobs, 6, reward: true, ct);
}
