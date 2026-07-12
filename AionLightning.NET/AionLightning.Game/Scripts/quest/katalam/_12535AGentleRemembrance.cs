// Port of Java data/scripts/system/handlers/quest/katalam/_12535AGentleRemembrance.java (Romanz).
// Talk to 801008 to start; using object 701738 three times advances var 0->1->2->3 (last one
// flips to REWARD) via UseQuestObjectAsync (Java's dieObject flag is accepted but ignored — no
// NPC controller/death-trigger infra wired to quest scripts); turn in at 801008.
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

namespace Quest.Katalam;

public sealed class _12535AGentleRemembrance : QuestHandlerBase
{
    private const int QuestIdConst = 12535;
    private const int NpcId        = 801008;
    private const int ObjectNpc    = 701738;

    public _12535AGentleRemembrance(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(NpcId).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(NpcId).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ObjectNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != NpcId) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
                return await SendQuestStartDialogAsync(env, conn, ct);
            return false;
        }

        if (targetId == ObjectNpc)
        {
            if (dialog != DialogAction.USE_OBJECT) return false;
            int var = entry.GetVar(0);
            return var switch
            {
                0 => await UseQuestObjectAsync(env, conn, 0, 1, reward: false, dieObject: true, ct),
                1 => await UseQuestObjectAsync(env, conn, 1, 2, reward: false, dieObject: true, ct),
                2 => await UseQuestObjectAsync(env, conn, 2, 3, reward: true, dieObject: true, ct),
                _ => false,
            };
        }

        if (entry.Status == QuestStatus.REWARD && targetId == NpcId)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
