// Port of Java data/scripts/system/handlers/quest/inggison/_11107ComfortisaBox.java.
// Talk to 798963 to start (accept gives 3x item 182206859, ignoring give failure); three relays
// (296489 var0, 296490 var1, 296491 var2) each consume one and advance, the last flipping to
// reward; turn in back at 798963.
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

public sealed class _11107ComfortisaBox : QuestHandlerBase
{
    private const int QuestIdConst = 11107;
    private const int StartNpc     = 798963;
    private const int Relay1Npc    = 296489;
    private const int Relay2Npc    = 296490;
    private const int Relay3Npc    = 296491;
    private const int MessageItem  = 182206859;

    private readonly IItemDao _itemDao;

    public _11107ComfortisaBox(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
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
        engine.RegisterQuestNpc(Relay3Npc).OnTalk.Add(QuestId);
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
                if (!await GiveQuestItemAsync(player, conn, _itemDao, MessageItem, 3, ct))
                    return true;
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry is null) return false;

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == StartNpc)
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

        int var = entry.GetVar(0);
        if (targetId == Relay1Npc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO1 && var == 0)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, MessageItem, 1, ct);
                return await DefaultCloseDialogAsync(env, conn, 0, 1, reward: false, sameNpc: false, ct);
            }
        }
        else if (targetId == Relay2Npc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SETPRO2 && var == 1)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, MessageItem, 1, ct);
                return await DefaultCloseDialogAsync(env, conn, 1, 2, reward: false, sameNpc: false, ct);
            }
        }
        else if (targetId == Relay3Npc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 2)
                return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            if (dialog == DialogAction.SETPRO3 && var == 2)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, MessageItem, 1, ct);
                return await DefaultCloseDialogAsync(env, conn, 2, 3, reward: true, sameNpc: false, ct);
            }
        }
        return false;
    }
}
