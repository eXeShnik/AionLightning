// Port of Java data/scripts/system/handlers/quest/inggison/_11106RewritingHistory.java.
// Talk to 798976 to start (accept gives item 182206780, ignoring give failure); relay at 798978
// (var 0) swaps 182206780 for 182206781; relay at 798979 (var 1) swaps 182206781 for 182206782 and
// flips to reward; turn in at 203832 (a different npc than the last relay).
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Inggison;

public sealed class _11106RewritingHistory : QuestHandlerBase
{
    private const int QuestIdConst = 11106;
    private const int StartNpc     = 798976;
    private const int Relay1Npc    = 798978;
    private const int Relay2Npc    = 798979;
    private const int TurnInNpc    = 203832;
    private const int Item1        = 182206780;
    private const int Item2        = 182206781;
    private const int Item3        = 182206782;

    private readonly IItemDao _itemDao;

    public _11106RewritingHistory(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay1Npc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay2Npc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartNpc && (entry is null || entry.Status == QuestStatus.NONE))
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                if (!await GiveQuestItemAsync(player, conn, _itemDao, Item1, 1, ct))
                    return true;
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry is null) return false;

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == TurnInNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        else if (entry.Status != QuestStatus.START)
        {
            return false;
        }

        if (targetId == Relay1Npc)
        {
            if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO1 && entry.GetVar(0) == 0)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, Item1, 1, ct);
                if (await GiveQuestItemAsync(player, conn, _itemDao, Item2, 1, ct))
                    entry.SetVar(0, 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
        }
        else if (targetId == Relay2Npc)
        {
            if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 1)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SETPRO2 && entry.GetVar(0) == 1)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, Item2, 1, ct);
                if (await GiveQuestItemAsync(player, conn, _itemDao, Item3, 1, ct))
                    entry.SetVar(0, 2);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
        }
        return false;
    }
}
