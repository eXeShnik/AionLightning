// Port of Java data/scripts/system/handlers/quest/morheim/_2367APrizedPossession.java.
// Start at 204339; SELECT_ACTION_1012/1097 grant a keepsake item along the way; SETPRO10/20 pick a
// reward tier (var 10 or 20) and flip to REWARD; turning in at 798079 or 798080 finishes with that
// tier (Java's sendQuestEndDialog(env, reward) two-step dialog). Reward index is derived from the
// persisted var (10 -> 0, 20 -> 1) rather than Java's shared instance field.
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

public sealed class _2367APrizedPossession : QuestHandlerBase
{
    private const int QuestIdConst = 2367;
    private const int StartNpc     = 204339;
    private const int FirstTurnIn  = 798079;
    private const int SecondTurnIn = 798080;
    private const int KeepsakeItem = 182204147;

    private readonly IItemDao _itemDao;

    public _2367APrizedPossession(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
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
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == StartNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
                case DialogAction.SELECT_ACTION_1012:
                    await GiveQuestItemAsync(player, conn, _itemDao, KeepsakeItem, 1, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1012, ct);
                case DialogAction.SELECT_ACTION_1097:
                    await GiveQuestItemAsync(player, conn, _itemDao, KeepsakeItem, 1, ct);
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
            if (targetId == FirstTurnIn && dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (targetId == SecondTurnIn && dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);

            return await SendQuestEndDialogWithRewardAsync(env, conn, entry.GetVar(0) == 20 ? 1 : 0, ct);
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
