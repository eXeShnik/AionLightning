// Port of Java data/scripts/system/handlers/quest/morheim/_2448ChaomirkSendsForHelp.java.
// Chaomirk (798115) grants the item on accept (Java sendQuestStartDialog(env, itemId, count),
// inlined here); SETPRO10/20 pick a reward tier (var 10 or 20) and flip to REWARD; turning in at
// either 798080 or 798079 removes the carried item and finishes with that tier (Java's
// sendQuestEndDialog(env, reward) two-step dialog). Reward index is derived from the persisted var
// (10 -> 0, 20 -> 1) rather than Java's shared instance field — see _2411EarthSpiritWaterSpirit for
// the same fix.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Morheim;

public sealed class _2448ChaomirkSendsForHelp : QuestHandlerBase
{
    private const int QuestIdConst = 2448;
    private const int ChaomirkNpc  = 798115;
    private const int FirstTurnIn  = 798080;
    private const int SecondTurnIn = 798079;
    private const int StartItemId  = 182204210;

    private readonly IItemDao _itemDao;

    public _2448ChaomirkSendsForHelp(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(ChaomirkNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(ChaomirkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FirstTurnIn).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SecondTurnIn).OnTalk.Add(QuestId);
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
            if (targetId != ChaomirkNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);

            switch (dialog)
            {
                case DialogAction.QUEST_ACCEPT or DialogAction.QUEST_ACCEPT_1:
                    if (!await StartMissionAsync(conn, player, QuestStatus.START, ct)) return false;
                    await GiveQuestItemAsync(player, conn, _itemDao, StartItemId, 1, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
                case DialogAction.QUEST_REFUSE or DialogAction.QUEST_REFUSE_1 or DialogAction.QUEST_REFUSE_2 or DialogAction.QUEST_REFUSE_SIMPLE:
                    return await SendQuestDialogAsync(conn, targetObjId, 0, ct);
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.START && targetId == ChaomirkNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
                case DialogAction.SELECT_ACTION_1012:
                    return await SendQuestDialogAsync(conn, targetObjId, 1012, ct);
                case DialogAction.SELECT_ACTION_1097:
                    return await SendQuestDialogAsync(conn, targetObjId, 1097, ct);
                case DialogAction.SETPRO10:
                    await ChangeQuestStepAsync(conn, entry, 0, 10, toReward: true, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                case DialogAction.SETPRO20:
                    await ChangeQuestStepAsync(conn, entry, 0, 20, toReward: true, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            int var = entry.GetVar(0);
            if (targetId == FirstTurnIn && var == 10 && dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (targetId == SecondTurnIn && var == 20 && dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);

            await RemoveQuestItemAsync(player, conn, _itemDao, StartItemId, 1, ct);
            return await SendQuestEndDialogWithRewardAsync(env, conn, var == 20 ? 1 : 0, ct);
        }
        return false;
    }

    /// <summary>Java QuestHandler.sendQuestEndDialog(env, reward): SELECT_QUEST_REWARD/USE_OBJECT
    /// shows the tier-confirm page 5+reward; SELECTED_QUEST_REWARDn/NOREWARD actually completes
    /// with that reward tier.</summary>
    private async ValueTask<bool> SendQuestEndDialogWithRewardAsync(QuestEnv env, GsClientConnection conn, int rewardIndex, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.REWARD) return false;
        int targetObjId = env.Target?.ObjectId ?? 0;
        int dialogId = env.DialogId;

        if (dialogId >= (int)DialogAction.SELECTED_QUEST_REWARD1 && dialogId <= (int)DialogAction.SELECTED_QUEST_NOREWARD)
        {
            if (!await FinishQuestAsync(conn, env.Player, rewardIndex, ct)) return false;
            await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
            return true;
        }
        if (dialogId == (int)DialogAction.SELECT_QUEST_REWARD || dialogId == (int)DialogAction.USE_OBJECT)
            return await SendQuestDialogAsync(conn, targetObjId, 5 + rewardIndex, ct);
        return false;
    }
}
