// Port of Java data/scripts/system/handlers/quest/sanctum/_3969SexiestManAlive.java (Rolandas).
// Requires quest 3968 already COMPLETE. Talk to Palentine (798390) to start (gives item
// 182206126, note: start dialog gated on USE_OBJECT not QUEST_SELECT, matching Java exactly);
// deliver the item to Andu (798391, var 0->1, reward); return to Palentine to finish.
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

public sealed class _3969SexiestManAlive : QuestHandlerBase
{
    private const int QuestIdConst = 3969;
    private const int PrecedingQuestId = 3968;
    private const int StartNpc     = 798390;
    private const int AnduNpc      = 798391;
    private const int LetterItemId = 182206126;

    private readonly IItemDao _itemDao;

    public _3969SexiestManAlive(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(AnduNpc).OnTalk.Add(QuestId);
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
                if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
                {
                    if (await GiveQuestItemAsync(player, conn, _itemDao, LetterItemId, 1, ct))
                        return await SendQuestStartDialogAsync(env, conn, ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }

        if (entry is null) return false;

        if (targetId == AnduNpc)
        {
            if (entry.Status == QuestStatus.START && entry.GetVar(0) == 0)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    if ((player.Inventory.FindByItemId(LetterItemId)?.Count ?? 0) > 0)
                    {
                        await RemoveQuestItemAsync(player, conn, _itemDao, LetterItemId, 1, ct);
                        entry.SetVar(0, entry.GetVar(0) + 1);
                        entry.Status = QuestStatus.REWARD;
                        await UpdateQuestStatusAsync(conn, entry, ct);
                    }
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
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
