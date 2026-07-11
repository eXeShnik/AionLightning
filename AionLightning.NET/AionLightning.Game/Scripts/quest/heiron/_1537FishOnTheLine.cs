// Port of Java data/scripts/system/handlers/quest/heiron/_1537FishOnTheLine.java.
// Talk to Rotgut (204588) to start; use three baited hooks (730189/730190/730191) in sequence
// (var 0->1->2->3, reward), turn in at Rotgut.
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

namespace Quest.Heiron;

public sealed class _1537FishOnTheLine : QuestHandlerBase
{
    private const int QuestIdConst = 1537;
    private const int StartNpc     = 204588;
    private const int HookOne      = 730189;
    private const int HookTwo      = 730190;
    private const int HookThree    = 730191;

    public _1537FishOnTheLine(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HookOne).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HookTwo).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HookThree).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, env.Target?.ObjectId ?? 0, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            switch (targetId)
            {
                case HookOne when dialog == DialogAction.USE_OBJECT:
                    return await UseQuestObjectAsync(env, conn, 0, 1, false, 0, ct);
                case HookTwo when dialog == DialogAction.USE_OBJECT:
                    return await UseQuestObjectAsync(env, conn, 1, 2, false, 0, ct);
                case HookThree when entry.GetVar(0) == 2 && dialog == DialogAction.USE_OBJECT:
                    await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: true, ct);
                    return true;
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
