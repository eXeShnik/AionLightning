// Port of Java data/scripts/system/handlers/quest/pandaemonium/_2916ManInTheLongBlackRobe.java
// (Cheatkiller). Accept at 204141, then a talk chain 204152 (var0 0->1) -> 204150 (1->2) ->
// 204151 (2->3) -> 798033 (3->4) -> 203673 (4->5); approaching 700211 (onAtDistance, var0 5->6)
// advances; return to 204141 which checks the collected quest items (var0 6, reward). Turn in at 204141.
// Note: the 204151 talk branch is kept faithful to Java even though Java's register() never registers
// 204151 as a quest npc (dead dialog case in the original).
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

namespace Quest.Pandaemonium;

public sealed class _2916ManInTheLongBlackRobe : QuestHandlerBase
{
    private const int QuestIdConst = 2916;
    private const int Npc204141    = 204141;
    private const int Npc204152    = 204152;
    private const int Npc204150    = 204150;
    private const int Npc204151    = 204151;
    private const int Npc798033    = 798033;
    private const int Npc203673    = 203673;
    private const int Npc700211    = 700211;

    private readonly IItemDao _itemDao;

    public _2916ManInTheLongBlackRobe(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(Npc204141).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Npc204141).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc204152).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc204150).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc798033).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc203673).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc700211).OnTalk.Add(QuestId);
        RegisterOnAtDistance(engine, Npc700211);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == Npc204141)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }
        else if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == Npc204152)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0)
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                }
                else if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
            else if (targetId == Npc204150)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 1)
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                }
                else if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            }
            else if (targetId == Npc204151)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 2)
                        return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                }
                else if (dialog == DialogAction.SETPRO3)
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
            }
            else if (targetId == Npc798033)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 3)
                        return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                }
                else if (dialog == DialogAction.SETPRO4)
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
            }
            else if (targetId == Npc203673)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 4)
                        return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                }
                else if (dialog == DialogAction.SETPRO5)
                    return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
            }
            else if (targetId == Npc700211)
            {
                if (var == 6)
                    return true;
            }
            else if (targetId == Npc204141)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 6)
                        return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                }
                else if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 6, 6, true, 5, 3143, ct);
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == Npc204141)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnAtDistanceAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is not null && entry.Status == QuestStatus.START && entry.GetVar(0) == 5)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 6, toReward: false, ct);
            return true;
        }
        return false;
    }
}
