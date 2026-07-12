// Port of Java data/scripts/system/handlers/quest/inggison/_11009MeiriaFriendlySuggestion.java.
// Talk to 798945 to start (accept gives 2x item 182206711, ignoring give failure); relay at 799008
// (var 0) consumes one; relay at 799017 (var 1) consumes one and gives item 182206712 (Java's
// return value there tracks the give's success/failure even though the dialog+status update
// already happened unconditionally — ported the same way); turn-in trigger at 798941 (var 2)
// consumes 182206712 and flips to reward; turn in back at 798945.
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

public sealed class _11009MeiriaFriendlySuggestion : QuestHandlerBase
{
    private const int QuestIdConst = 11009;
    private const int StartNpc     = 798945;
    private const int Relay1Npc    = 799008;
    private const int Relay2Npc    = 799017;
    private const int TurnInNpc    = 798941;
    private const int LetterItem   = 182206711;
    private const int ReplyItem    = 182206712;

    private readonly IItemDao _itemDao;

    public _11009MeiriaFriendlySuggestion(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
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

        if (targetId == StartNpc && entry is null)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                if (!await GiveQuestItemAsync(player, conn, _itemDao, LetterItem, 2, ct))
                    return true;
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry is null) return false;

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == Relay1Npc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, LetterItem, 1, ct);
                    entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
            }
            else if (targetId == Relay2Npc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, LetterItem, 1, ct);
                    entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    return await GiveQuestItemAsync(player, conn, _itemDao, ReplyItem, 1, ct);
                }
            }
            else if (targetId == TurnInNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO3)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, ReplyItem, 1, ct);
                    entry.SetVar(0, 3);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }
}
