// Hand-written quest script (Phase 5 Batch 0 golden exemplar). Port of Java
// data/scripts/system/handlers/quest/poeta/_1003IllegalLogging.java.
//
// Dialog + kill quest: talk to the start NPC, kill 6 woodcutters (var 1->7), talk again to
// advance past var 7, then kill the boss twice more (var 8->10) before it flips to REWARD.
//
// Script-authoring convention (see QuestEngineHostedService.LoadHandWrittenScripts): subclass
// QuestHandlerBase, hardcode the quest id as a compile-time constant passed to the base
// constructor, and expose exactly one public constructor shaped
// (IDataManager, IQuestDao, QuestRewardService, IItemDao) — the batch-compile host resolves and
// invokes that fixed constructor by reflection. This script doesn't touch items, so IItemDao is
// declared and simply unused.
using System.Linq;
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

namespace Quest.Poeta;

public sealed class _1003IllegalLogging : QuestHandlerBase
{
    private const int QuestIdConst = 1003;
    private const int StartNpcId   = 203081;
    private const int BossNpcId    = 210160; // has its own kill-range/reward-flip behavior, kept out of _woodcutterNpcIds

    private static readonly int[] _woodcutterNpcIds =
        [210096, 210149, 210145, 210146, 210150, 210151, 210092, 210154, 210685];

    public _1003IllegalLogging(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpcId).OnTalk.Add(QuestId);
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npcId in _woodcutterNpcIds)
            engine.RegisterQuestNpc(npcId).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(BossNpcId).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1100, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;

        if (entry.Status == QuestStatus.REWARD)
            return await SendQuestEndDialogAsync(env, conn, ct);

        if (entry.Status != QuestStatus.START || env.TargetId != StartNpcId) return false;

        int var         = entry.GetVar(0);
        int targetObjId = env.Target?.ObjectId ?? 0;

        switch (DialogActionLookup.FromId(env.DialogId))
        {
            case DialogAction.QUEST_SELECT when var == 0:
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            case DialogAction.QUEST_SELECT when var == 13:
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            case DialogAction.SETPRO1 or DialogAction.SETPRO2 when var is 0 or 7:
                entry.SetVar(0, var + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            default:
                return false;
        }
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int targetId = env.TargetId;
        if (_woodcutterNpcIds.Contains(targetId))
            return await DefaultOnKillEventAsync(env, conn, _woodcutterNpcIds, startVar: 1, endVar: 7, ct);

        if (targetId != BossNpcId) return false;

        int var = entry.GetVar(0);
        if (var is >= 8 and <= 9)
        {
            await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
            return true;
        }
        if (var == 10)
        {
            await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
            return true;
        }
        return false;
    }
}
