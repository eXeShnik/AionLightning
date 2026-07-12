// Port of Java data/scripts/system/handlers/quest/brusthonin/_4004TheSeedsOfHope.java.
// Talk to Randet (205128) to start; use the Earth Mound (700340) 5 times (var 0->4, the 5th use
// flips to REWARD); report back to Randet.
// Skip vs Java: useQuestObject's dieObject param is a documented no-op in this port (no NPC
// controller death/respawn infra) — see QuestHandlerBase.UseQuestObjectAsync.
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

namespace Quest.Brusthonin;

public sealed class _4004TheSeedsOfHope : QuestHandlerBase
{
    private const int QuestIdConst = 4004;
    private const int RandetNpc    = 205128;
    private const int EarthMound   = 700340;

    public _4004TheSeedsOfHope(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(RandetNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(RandetNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EarthMound).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId == RandetNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == RandetNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == EarthMound)
        {
            int var = entry.GetVar(0);
            if (dialog == DialogAction.USE_OBJECT)
            {
                if (var < 4)
                    return await UseQuestObjectAsync(env, conn, var, var + 1, false, true, ct);
                if (var == 4)
                    return await UseQuestObjectAsync(env, conn, 4, 4, true, true, ct);
            }
        }
        return false;
    }
}
