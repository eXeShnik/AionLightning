// Port of Java data/scripts/system/handlers/quest/brusthonin/_4038AlasPoorGroznak.java.
// Talk to Surt (205150) to start; interact with Groznak's Skull (730155) through var 0->1->2,
// handing in the quest_data.xml collect items at var 1, then finish at var 2 (reward); the 3
// skeleton flavor objects (700380/700381/700382) are inert "loot" acknowledgements at var 1.
// Java's switch(dialog) at 730155 fell through from QUEST_SELECT into the next case without a
// break; each case is guarded by its own var check so the fallthrough was a no-op — ported as
// plain var-scoped if-checks with identical reachable behavior.
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

public sealed class _4038AlasPoorGroznak : QuestHandlerBase
{
    private const int QuestIdConst = 4038;
    private const int SurtNpc      = 205150;
    private const int SkullObj     = 730155;
    private static readonly int[] _skeletonObjs = [700380, 700381, 700382];

    private readonly IItemDao _itemDao;

    public _4038AlasPoorGroznak(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(SurtNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(SurtNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SkullObj).OnTalk.Add(QuestId);
        foreach (int npc in _skeletonObjs)
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
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
            if (targetId == SurtNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == SkullObj)
            {
                if (dialog == DialogAction.FINISH_DIALOG)
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                if (var == 0)
                {
                    if (dialog == DialogAction.QUEST_SELECT)
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (dialog == DialogAction.SETPRO1)
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                else if (var == 1)
                {
                    if (dialog == DialogAction.QUEST_SELECT)
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (env.DialogId == (int)DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                        return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 2, false, 10000, 10001, ct);
                }
                else if (var == 2)
                {
                    if (dialog == DialogAction.SETPRO3)
                        return await DefaultCloseDialogAsync(env, conn, 2, 2, reward: true, sameNpc: false, ct);
                }
                return false;
            }

            if (var == 1)
            {
                foreach (int npc in _skeletonObjs)
                {
                    if (targetId == npc) return true; // loot
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == SurtNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
