// Port of Java data/scripts/system/handlers/quest/katalam/_22535RemembertheFallen.java (Romanz).
// Asmodian sibling of _12535AGentleRemembrance. Accepting at 800557 grants 3x item 182213350;
// using object 701746 three times consumes one each time and advances var 0->1->2->3 (last one
// flips to REWARD) via UseQuestObjectAsync's built-in item-removal param (Java's dieObject flag
// is accepted but ignored — no NPC controller/death-trigger infra wired to quest scripts); turn
// in at 800557.
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

public sealed class _22535RemembertheFallen : QuestHandlerBase
{
    private const int QuestIdConst = 22535;
    private const int NpcId        = 800557;
    private const int ObjectNpc    = 701746;
    private const int TokenItemId  = 182213350;

    private readonly IItemDao _itemDao;

    public _22535RemembertheFallen(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(NpcId).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(NpcId).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ObjectNpc).OnTalk.Add(QuestId);
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
            if (targetId != NpcId) return false;
            if (dialog == DialogAction.QUEST_SELECT)
            {
                await GiveQuestItemAsync(player, conn, _itemDao, TokenItemId, 3, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            }
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
                0 => await UseQuestObjectAsync(env, conn, 0, 1, false, 0, 0, 0, TokenItemId, 1, 0, true, _itemDao, ct),
                1 => await UseQuestObjectAsync(env, conn, 1, 2, false, 0, 0, 0, TokenItemId, 1, 0, true, _itemDao, ct),
                2 => await UseQuestObjectAsync(env, conn, 2, 3, true, 0, 0, 0, TokenItemId, 1, 0, true, _itemDao, ct),
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
