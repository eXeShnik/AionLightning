// Port of Java data/scripts/system/handlers/quest/chantra_dredgion/_4721RiseOfChantraDredgion.java.
// Elyos mirror of _3721DisarmTheChantraDredgion: start/turn-in at Valetta (799226); relay at Yorgen
// (799403, var0 0->1); loot npc 700948 drops item 182205691 at var0==1, picking it up bumps var0
// 1->2 (no reward); killing 216886 at var0==2 flips straight to REWARD. Same Java switch-fallthrough
// note as the Asmodian sibling applies here (net no-op, see that file's header for the rationale).
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

namespace Quest.ChantraDredgion;

public sealed class _4721RiseOfChantraDredgion : QuestHandlerBase
{
    private const int QuestIdConst = 4721;
    private const int ValettaNpc   = 799226;
    private const int YorgenNpc    = 799403;
    private const int LootNpc      = 700948;
    private const int KillNpc      = 216886;
    private const int ItemId       = 182205691;

    public _4721RiseOfChantraDredgion(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(ValettaNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(ValettaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(YorgenNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KillNpc).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(LootNpc).OnTalk.Add(QuestId);
        RegisterQuestDrop(engine, LootNpc, ItemId, 1, 100, 1);
        engine.RegisterItemGet(ItemId, QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != ValettaNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == YorgenNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
            else if (targetId == LootNpc)
            {
                return true;
            }
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == ValettaNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnItemGetAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != ItemId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
        return true;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, KillNpc, 2, reward: true, ct);
}
