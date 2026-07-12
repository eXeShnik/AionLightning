// Port of Java data/scripts/system/handlers/quest/inggison/_11012PracticalNursing.java.
// Talk to 799071 to start (accept gives 3x item 182206715, ignoring give failure); three relays
// (799072 var0, 799073 var1, 799074 var2) each consume one dose and advance, the last flipping to
// reward; turn in back at 799071.
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

public sealed class _11012PracticalNursing : QuestHandlerBase
{
    private const int QuestIdConst = 11012;
    private const int StartNpc     = 799071;
    private const int Relay1Npc    = 799072;
    private const int Relay2Npc    = 799073;
    private const int Relay3Npc    = 799074;
    private const int DoseItem     = 182206715;

    private readonly IItemDao _itemDao;

    public _11012PracticalNursing(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
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

        if (targetId == StartNpc && entry is null)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                if (!await GiveQuestItemAsync(player, conn, _itemDao, DoseItem, 3, ct))
                    return true;
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry is null) return false;

        int var = entry.GetVar(0);
        if (entry.Status == QuestStatus.START)
        {
            if (targetId == Relay1Npc && var == 0)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 0, 1, reward: false, sameNpc: false,
                        giveItemId: 0, giveItemCount: 0, removeItemId: DoseItem, removeItemCount: 1, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (targetId == Relay2Npc && var == 1)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 1, 2, reward: false, sameNpc: false,
                        giveItemId: 0, giveItemCount: 0, removeItemId: DoseItem, removeItemCount: 1, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (targetId == Relay3Npc && var == 2)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 2, 3, reward: true, sameNpc: false,
                        giveItemId: 0, giveItemCount: 0, removeItemId: DoseItem, removeItemCount: 1, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }
}
