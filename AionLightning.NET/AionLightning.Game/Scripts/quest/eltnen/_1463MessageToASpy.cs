// Port of Java data/scripts/system/handlers/quest/eltnen/_1463MessageToASpy.java (Balthazar).
// Talk to 203940 to start; 203903 hands the message item (182201382) on SETPRO1; 204424 swaps it
// for a reply item (182201383) on SETPRO2; return to 203903 to finish.
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

namespace Quest.Eltnen;

public sealed class _1463MessageToASpy : QuestHandlerBase
{
    private const int QuestIdConst  = 1463;
    private const int StartNpc      = 203940;
    private const int MessengerNpc  = 203903;
    private const int RecipientNpc  = 204424;
    private const int MessageItemId = 182201382;
    private const int ReplyItemId   = 182201383;

    private readonly IItemDao _itemDao;

    public _1463MessageToASpy(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MessengerNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RecipientNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        var entry = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == MessengerNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 0:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.QUEST_SELECT when var == 2:
                        return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    case DialogAction.SETPRO1:
                    {
                        var existing = player.Inventory.FindByItemId(MessageItemId);
                        if (existing is null or { Count: 0 } && !await GiveQuestItemAsync(player, conn, _itemDao, MessageItemId, 1, ct))
                            return true;
                        entry.SetVar(0, var + 1);
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                        return true;
                    }
                    case DialogAction.SELECT_QUEST_REWARD:
                        entry.SetVar(0, 3);
                        await RemoveQuestItemAsync(player, conn, _itemDao, ReplyItemId, 1, ct);
                        entry.Status = QuestStatus.REWARD;
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await SendQuestEndDialogAsync(env, conn, ct);
                    default:
                        return false;
                }
            }
            if (targetId == RecipientNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 1:
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    case DialogAction.SETPRO2:
                    {
                        entry.SetVar(0, var + 1);
                        await RemoveQuestItemAsync(player, conn, _itemDao, MessageItemId, 1, ct);
                        var reply = player.Inventory.FindByItemId(ReplyItemId);
                        if (reply is null or { Count: 0 } && !await GiveQuestItemAsync(player, conn, _itemDao, ReplyItemId, 1, ct))
                            return true;
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                        return true;
                    }
                    default:
                        return await SendQuestEndDialogAsync(env, conn, ct);
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == MessengerNpc)
        {
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
