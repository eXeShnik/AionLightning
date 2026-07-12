// Port of Java data/scripts/system/handlers/quest/daevation/_2988TheWiseInDisguise.java
// (Asmodian mirror of _1988AMeetingWithASage). Talk to Heimdall (204182) to start; relay through
// Utgar (204338, var0 0->1) and Brakan (204213, var0 1->2); at Kanensa (204146) a SELECT_ACTION_2035
// "confirm" click peeks whether the sage's item (186000039) is held before allowing the turn-in
// (var0 stays 2, REWARD, item consumed). A second Kanensa visit while REWARD shows dialog 4080
// then finishes normally.
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

namespace Quest.Daevation;

public sealed class _2988TheWiseInDisguise : QuestHandlerBase
{
    private const int QuestIdConst = 2988;
    private const int Heimdall     = 204182;
    private const int Utgar        = 204338;
    private const int Brakan       = 204213;
    private const int Kanensa      = 204146;
    private const int SageItem     = 186000039;

    private readonly IItemDao _itemDao;

    public _2988TheWiseInDisguise(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        var heimdall = engine.RegisterQuestNpc(Heimdall);
        heimdall.OnQuestStart.Add(QuestId);
        heimdall.OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Utgar).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Brakan).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Kanensa).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var entry       = player.Quests.Get(QuestId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != Heimdall) return false;
            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (targetId == Utgar)
        {
            if (entry.Status != QuestStatus.START || entry.GetVar(0) != 0) return false;

            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (targetId == Brakan)
        {
            if (entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;

            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.SETPRO2)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (targetId == Kanensa)
        {
            if (entry.Status == QuestStatus.START && entry.GetVar(0) == 2)
            {
                if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);

                if (DialogActionLookup.FromId(env.DialogId) == DialogAction.SELECT_ACTION_2035)
                {
                    var sageItem = player.Inventory.FindByItemId(SageItem);
                    return await SendQuestDialogAsync(conn, targetObjId, (sageItem?.Count ?? 0) > 0 ? 2035 : 2120, ct);
                }

                if (DialogActionLookup.FromId(env.DialogId) == DialogAction.SELECT_QUEST_REWARD)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 2, 2, reward: true, sameNpc: false,
                        giveItemId: 0, giveItemCount: 0, removeItemId: SageItem, removeItemCount: 1, ct);

                return await SendQuestStartDialogAsync(env, conn, ct);
            }

            if (entry.Status == QuestStatus.REWARD)
            {
                if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4080, ct);

                if (DialogActionLookup.FromId(env.DialogId) == DialogAction.SELECT_QUEST_REWARD)
                {
                    entry.SetVar(0, 8);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }

        return false;
    }
}
