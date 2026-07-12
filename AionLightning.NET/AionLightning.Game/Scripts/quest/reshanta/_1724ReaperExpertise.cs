// Port of Java data/scripts/system/handlers/quest/reshanta/_1724ReaperExpertise.java (Hilgert).
// Start at 278519 (gives item 182203131 on QUEST_REFUSE_1 — Java's own accept trigger for this
// quest, not QUEST_ACCEPT_1; ported as-is). Relay 278591 (SETPRO1) -> 278599 (SETPRO2, gives item
// 182202152, flips to REWARD). Turn in at 278594.
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

namespace Quest.Reshanta;

public sealed class _1724ReaperExpertise : QuestHandlerBase
{
    private const int QuestIdConst = 1724;
    private const int StartNpc     = 278519;
    private const int RelayNpc1    = 278591;
    private const int RelayNpc2    = 278599;
    private const int TurnInNpc    = 278594;
    private const int StartItemId  = 182203131;
    private const int RelayItemId  = 182202152;

    private readonly IItemDao _itemDao;

    public _1724ReaperExpertise(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(RelayNpc1).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RelayNpc2).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartNpc)
        {
            if (entry is null)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (env.DialogId == (int)DialogAction.QUEST_REFUSE_1)
                {
                    if (await GiveQuestItemAsync(player, conn, _itemDao, StartItemId, 1, ct))
                        return await SendQuestStartDialogAsync(env, conn, ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }
        else if (entry is { Status: QuestStatus.START })
        {
            if (targetId == RelayNpc1)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    entry.SetVar(0, entry.GetVar(0) + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
            }
            else if (targetId == RelayNpc2)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, RelayItemId, 1, ct))
                        return true;
                    entry.SetVar(0, entry.GetVar(0) + 1);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
            }
        }
        else if (entry is { Status: QuestStatus.REWARD } && targetId == TurnInNpc)
        {
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
