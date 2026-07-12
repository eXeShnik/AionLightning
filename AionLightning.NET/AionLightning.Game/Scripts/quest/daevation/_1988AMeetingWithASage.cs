// Port of Java data/scripts/system/handlers/quest/daevation/_1988AMeetingWithASage.java.
// Talk to Leah (203725) to start; relay through Tumblusen (203989, var0 0->1) and Paorunerk
// (798018, var0 1->2); turn in at Fermina (203771, var0 stays 2, REWARD, consumes the sage's
// item 186000039). A second Fermina visit while REWARD shows dialog 4080 then finishes normally.
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

public sealed class _1988AMeetingWithASage : QuestHandlerBase
{
    private const int QuestIdConst = 1988;
    private const int Leah         = 203725;
    private const int Tumblusen    = 203989;
    private const int Paorunerk    = 798018;
    private const int Fermina      = 203771;
    private const int SageItem     = 186000039;

    private readonly IItemDao _itemDao;

    public _1988AMeetingWithASage(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        var leah = engine.RegisterQuestNpc(Leah);
        leah.OnQuestStart.Add(QuestId);
        leah.OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Tumblusen).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Paorunerk).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Fermina).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var entry       = player.Quests.Get(QuestId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != Leah) return false;
            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (targetId == Tumblusen)
        {
            if (entry.Status != QuestStatus.START || entry.GetVar(0) != 0) return false;

            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (targetId == Paorunerk)
        {
            if (entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;

            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.SETPRO2)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (targetId == Fermina)
        {
            if (entry.Status == QuestStatus.START && entry.GetVar(0) == 2)
            {
                if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
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
