// Port of Java data/scripts/system/handlers/quest/sanctum/_3967AndusDyeBox.java (Rolandas).
// Requires quest 3966 already COMPLETE. Talk to Andu (798391) to start (note: the start dialog is
// gated on USE_OBJECT rather than QUEST_SELECT here, matching Java exactly); Arenzes (798309)
// gives item 182206122 and flips straight to REWARD; return to Andu to finish.
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

namespace Quest.Sanctum;

public sealed class _3967AndusDyeBox : QuestHandlerBase
{
    private const int QuestIdConst = 3967;
    private const int PrecedingQuestId = 3966;
    private const int StartNpc     = 798391;
    private const int ArenzesNpc   = 798309;
    private const int DyeBoxItemId = 182206122;

    private readonly IItemDao _itemDao;

    public _3967AndusDyeBox(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(ArenzesNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var preceding = player.Quests.Get(PrecedingQuestId);
        if (preceding is null || preceding.Status != QuestStatus.COMPLETE) return false;

        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartNpc)
        {
            if (entry is null || entry.Status == QuestStatus.NONE)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }

        if (entry is null) return false;

        if (targetId == ArenzesNpc)
        {
            if (entry.Status == QuestStatus.START && entry.GetVar(0) == 0)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    if (await GiveQuestItemAsync(player, conn, _itemDao, DyeBoxItemId, 1, ct))
                    {
                        entry.SetVar(0, entry.GetVar(0) + 1);
                        entry.Status = QuestStatus.REWARD;
                        await UpdateQuestStatusAsync(conn, entry, ct);
                    }
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId == StartNpc && entry.Status == QuestStatus.REWARD)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, DyeBoxItemId, 1, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
