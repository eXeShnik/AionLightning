// Port of Java data/scripts/system/handlers/quest/theobomos/_3093RecetteSecretedeQuenelles.java
// ("Secret Dumpling Recipe"). Accept at Bororinerk (798185), who grants a starter item (182206062);
// Gastak (798177, var 1->2) then Jabala (798179, var 2->3) advance the chain; Hestia (203784, var
// 3, grants the recipe 182208052 - result ignored, matching Java's empty `if (...) ;` - and flips
// to REWARD); turn-in has no npc gate in the Java source (any registered npc works while the quest
// is REWARD), reproduced as-is below.
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

namespace Quest.Theobomos;

public sealed class _3093RecetteSecretedeQuenelles : QuestHandlerBase
{
    private const int QuestIdConst  = 3093;
    private const int BororinerkNpc = 798185;
    private const int GastakNpc     = 798177;
    private const int JabalaNpc     = 798179;
    private const int HestiaNpc     = 203784;
    private const int StarterItemId = 182206062;
    private const int RecipeItemId  = 182208052;

    private readonly IItemDao _itemDao;

    public _3093RecetteSecretedeQuenelles(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(BororinerkNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(BororinerkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GastakNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(JabalaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HestiaNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != BororinerkNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                if (await GiveQuestItemAsync(player, conn, _itemDao, StarterItemId, 1, ct))
                    return await SendQuestStartDialogAsync(env, conn, ct);
                return false;
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, RecipeItemId, 1, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        if (entry.Status != QuestStatus.START) return false;
        int var = entry.GetVar(0);

        if (targetId == GastakNpc && var == 1)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                entry.SetVar(0, var + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
        }
        else if (targetId == JabalaNpc && var == 2)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SETPRO2)
            {
                entry.SetVar(0, var + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
        }
        else if (targetId == HestiaNpc && var == 3)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            if (dialog == DialogAction.SETPRO3)
            {
                await GiveQuestItemAsync(player, conn, _itemDao, RecipeItemId, 1, ct);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
        }
        return false;
    }
}
