// Port of Java data/scripts/system/handlers/quest/gelkmaros/_21081A_Helping_Hand.java (zhkchi).
// Talk to Richelle (799225), gives item 182214017 on accept; relay chain Brontes (799332, var
// 0->1) -> Pilipides (799217, var 1->2) -> Drenia (799202, SELECT_QUEST_REWARD flips to REWARD,
// same npc); turn in at Drenia.
// Java bug fixed: the original outer switch(targetId) had no break between the three npc cases,
// so an unmatched dialog at 799332 would fall through into 799217's (and then 799202's) case
// block. Each inner switch/case shape happens to be step-gated already so the fallthrough was
// inert in practice, but this port uses independent if-blocks per npc to avoid relying on that.
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

namespace Quest.Gelkmaros;

public sealed class _21081A_Helping_Hand : QuestHandlerBase
{
    private const int QuestIdConst = 21081;
    private const int RichelleNpc  = 799225;
    private const int BrontesNpc   = 799332;
    private const int PilipidesNpc = 799217;
    private const int DreniaNpc    = 799202;
    private const int ItemId       = 182214017;

    private readonly IItemDao _itemDao;

    public _21081A_Helping_Hand(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(RichelleNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(RichelleNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BrontesNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(PilipidesNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(DreniaNpc).OnTalk.Add(QuestId);
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
            if (targetId == RichelleNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
                {
                    if (await GiveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct))
                        return await SendQuestStartDialogAsync(env, conn, ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == BrontesNpc)
            {
                if (dialog is DialogAction.QUEST_SELECT or DialogAction.SELECT_ACTION_1353)
                    return await SendQuestDialogAsync(conn, targetObjId, 1353, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }

            if (targetId == PilipidesNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SELECT_ACTION_1694)
                    return await SendQuestDialogAsync(conn, targetObjId, 1694, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }

            if (targetId == DreniaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, reward: true, sameNpc: true, ct);
                return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == DreniaNpc)
        {
            if (env.DialogId == 1009)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
