// Port of Java data/scripts/system/handlers/quest/silentera_canyon/_30056DirvisiasSorrow.java (Ritsu).
// Preceded by quest 30055. Accepting from Gellius (798929) via QUEST_ACCEPT_1 first gives item
// 182209223 (x1); using the Statue of Dirvisia (700569) at var 0 spawns Dirvisia (799034) and
// consumes that item; talking to spawned Dirvisia's SETPRO1 flips straight to REWARD; turning in at
// Telemachus (203901) removes item 182209224 (x1) via the SELECT_QUEST_REWARD dialog branch before
// showing the closing text (dialog 5) - this branch is a Java quirk carried over faithfully: it
// does not call the shared reward/complete flow (Java's own sendQuestEndDialog/finishQuest), so
// (matching the original) the entry stays at REWARD status; anything other than USE_OBJECT/
// SELECT_QUEST_REWARD falls to the normal turn-in guard instead.
// Skip vs Java: the spawned Dirvisia's scheduled despawn/onDelete() 400ms after SETPRO1 is omitted -
// no NPC AI/controller subsystem in this port yet (same skip as the Poeta golden exemplar's
// Sleeping Elder); the quest var/status transition still completes normally.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.SilenteraCanyon;

public sealed class _30056DirvisiasSorrow : QuestHandlerBase
{
    private const int QuestIdConst   = 30056;
    private const int GelliusNpc     = 798929;
    private const int TelemachusNpc  = 203901;
    private const int StatueNpc      = 700569;
    private const int DirvisiaNpc    = 799034;
    private const int AcceptItem     = 182209223;
    private const int TurnInItem     = 182209224;
    private const int SilenteraWorldId = 600010000;

    private readonly IItemDao _itemDao;

    public _30056DirvisiasSorrow(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(GelliusNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(GelliusNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TelemachusNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StatueNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(DirvisiaNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != GelliusNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_1)
            {
                if (!await GiveQuestItemAsync(player, conn, _itemDao, AcceptItem, 1, ct))
                    return true;
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != TelemachusNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, TurnInItem, 1, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId == StatueNpc)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 0)
                {
                    SpawnQuestNpc(SilenteraWorldId, 1, DirvisiaNpc, 555.8842f, 307.8092f, 310.24997f, 0);
                    return await UseQuestObjectAsync(env, conn, 0, 0, reward: false, varNum: 0,
                        addItemId: 0, addItemCount: 0, removeItemId: AcceptItem, removeItemCount: 1,
                        movieId: 0, dieObject: false, _itemDao, ct);
                }
            }
            else if (targetId == DirvisiaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1 && var == 0)
                    return await DefaultCloseDialogAsync(env, conn, 0, 0, reward: true, sameNpc: false, ct);
            }
        }

        return false;
    }
}
