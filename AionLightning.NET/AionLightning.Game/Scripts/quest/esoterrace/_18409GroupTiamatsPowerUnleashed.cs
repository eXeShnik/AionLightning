// Port of Java data/scripts/system/handlers/quest/esoterrace/_18409GroupTiamatsPowerUnleashed.java
// (Ritsu). Start at 799553; 799552 bumps var 0 -> 1 via SETPRO1; kill 215795 at var 0 == 2 to flip to
// REWARD (giving 182215008, consuming 182215007); turn in at 799552. 730014 is registered for OnTalk
// but has no dialog branch in the Java source either - kept registered, simply inert.
// Java's onDialogEvent also has a branch for targetId 205232 (advancing var 0 from 1 -> 2, giving
// 182215007/consuming 182215006) that register() never wires up via registerQuestNpc - the engine
// never routes dialog events for an unregistered npc to this handler, so that branch is dead code in
// the original; kept for 1:1 parity, documented as unreachable rather than silently dropped.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Esoterrace;

public sealed class _18409GroupTiamatsPowerUnleashed : QuestHandlerBase
{
    private const int QuestIdConst  = 18409;
    private const int StartNpc      = 799553;
    private const int RelayNpc      = 799552;
    private const int InertNpc      = 730014;
    private const int DeadBranchNpc = 205232; // referenced by Java but never registered - dead branch
    private const int KillMob       = 215795;
    private const int GiveItem      = 182215007;
    private const int RemoveItem    = 182215006;
    private const int RewardItem    = 182215008;

    private readonly IItemDao _itemDao;

    public _18409GroupTiamatsPowerUnleashed(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RelayNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(InertNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KillMob).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartNpc)
        {
            if (entry is null || entry.Status == QuestStatus.NONE)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId == RelayNpc)
        {
            if (entry is not null && entry.Status == QuestStatus.START && entry.GetVar(0) == 0)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    entry.SetVar(0, entry.GetVar(0) + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (entry is not null && entry.Status == QuestStatus.REWARD)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId == DeadBranchNpc)
        {
            if (entry is not null && entry.Status == QuestStatus.START && entry.GetVar(0) == 1)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, GiveItem, 1, ct)) return true;
                    await RemoveQuestItemAsync(player, conn, _itemDao, RemoveItem, 1, ct);
                    entry.SetVar(0, entry.GetVar(0) + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.TargetId != KillMob) return false;

        if (entry.GetVar(0) == 2)
        {
            await GiveQuestItemAsync(player, conn, _itemDao, RewardItem, 1, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, GiveItem, 1, ct);
            entry.Status = QuestStatus.REWARD;
            await UpdateQuestStatusAsync(conn, entry, ct);
        }
        return false;
    }
}
