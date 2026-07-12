// Port of Java data/scripts/system/handlers/quest/silentera_canyon/_30156NepsLove.java (Ritsu).
// Preceded by quest 30155 (reward tier 0). Accepting from Nep (799234) via QUEST_ACCEPT_1 gives item
// 182209253 (x1); using the Statue of Sinigalla (700570) at var 0 spawns Sinigalla (799339) and
// consumes that item; talking to spawned Sinigalla's SETPRO1 flips straight to REWARD; turning in
// at Vili (204304) shows dialog 5 on SELECT_QUEST_REWARD without finishing the quest - a Java quirk
// carried over faithfully, matching the sibling _30056DirvisiasSorrow pattern (the entry stays at
// REWARD status; anything else falls to the normal turn-in guard).
// Java bug: the statue's useQuestObject call removed item 182209223 (the accept item from the
// unrelated _30056DirvisiasSorrow script, likely copy-pasted) instead of 182209253 - the item this
// quest actually hands out at accept - so the consumed item would never match what's in the
// player's bag. Changed to 182209253 to match the accept item, consistent with the sibling
// _30157VilisMind script's matching give/remove pair.
// Skip vs Java: the spawned Sinigalla's scheduled despawn/onDelete() 400ms after SETPRO1 is omitted -
// no NPC AI/controller subsystem in this port yet (same skip as _30056DirvisiasSorrow).
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

public sealed class _30156NepsLove : QuestHandlerBase
{
    private const int QuestIdConst   = 30156;
    private const int NepNpc         = 799234;
    private const int ViliNpc        = 204304;
    private const int StatueNpc      = 700570;
    private const int SinigallaNpc   = 799339;
    private const int AcceptItem     = 182209253;
    private const int SilenteraWorldId = 600010000;

    private readonly IItemDao _itemDao;

    public _30156NepsLove(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(NepNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(NepNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ViliNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StatueNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SinigallaNpc).OnTalk.Add(QuestId);
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
            if (targetId != NepNpc) return false;
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
            if (targetId != ViliNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId == StatueNpc)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 0)
                {
                    SpawnQuestNpc(SilenteraWorldId, 1, SinigallaNpc, 545.308f, 1232.3855f, 304.35193f, 73);
                    return await UseQuestObjectAsync(env, conn, 0, 0, reward: false, varNum: 0,
                        addItemId: 0, addItemCount: 0, removeItemId: AcceptItem, removeItemCount: 1,
                        movieId: 0, dieObject: false, _itemDao, ct);
                }
            }
            else if (targetId == SinigallaNpc)
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
