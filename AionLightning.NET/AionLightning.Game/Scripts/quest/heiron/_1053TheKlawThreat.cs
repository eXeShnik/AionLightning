// Port of Java data/scripts/system/handlers/quest/heiron/_1053TheKlawThreat.java.
// Talk to Senea (204583) to progress var0->1 then 1->3 (collect-item check dialog only, no state
// change); killing Klaw Spawn (700209) has a 1-in-5 chance to spawn Klaw Chief (212120), whose
// death at var 3 flips to REWARD. Mission-chain quest (no NPC quest-offer dialog), gated on 1500.
using System;
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

namespace Quest.Heiron;

public sealed class _1053TheKlawThreat : QuestHandlerBase
{
    private const int QuestIdConst = 1053;
    private const int SeneaNpc     = 204583;
    private const int EndNpc       = 204502;
    private const int KlawSpawnNpc = 700209;
    private const int KlawChiefNpc = 212120;

    private readonly IItemDao _itemDao;

    public _1053TheKlawThreat(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(KlawSpawnNpc).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(KlawChiefNpc).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(SeneaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EndNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 1500, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == EndNpc) return await SendQuestEndDialogAsync(env, conn, ct);
            return false;
        }
        if (entry.Status != QuestStatus.START) return false;
        if (targetId != SeneaNpc) return false;

        int var = entry.GetVar(0);
        if (dialog == DialogAction.QUEST_SELECT)
        {
            if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            return false;
        }
        if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
        {
            if (var != 1) return await SendQuestDialogAsync(conn, targetObjId, 10001, ct);
            return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 1, false, 10000, 10001, ct);
        }
        if (dialog == DialogAction.SELECT_ACTION_1693)
        {
            await PlayQuestMovieAsync(conn, env.Player, 186, ct);
            return false;
        }
        if (dialog == DialogAction.SETPRO1 && var == 0)
            return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
        if (dialog == DialogAction.SETPRO3 && var == 1)
            return await DefaultCloseDialogAsync(env, conn, 1, 3, ct);
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (env.TargetId == KlawSpawnNpc)
        {
            if (Random.Shared.Next(5) == 1 && env.Target is not null)
            {
                SpawnQuestNpc(210040000, 1, KlawChiefNpc,
                    env.Target.Position.X, env.Target.Position.Y, env.Target.Position.Z, 0);
                return true;
            }
        }
        else if (env.TargetId == KlawChiefNpc && entry.GetVar(0) == 3)
        {
            entry.Status = QuestStatus.REWARD;
            await UpdateQuestStatusAsync(conn, entry, ct);
        }
        return false;
    }
}
