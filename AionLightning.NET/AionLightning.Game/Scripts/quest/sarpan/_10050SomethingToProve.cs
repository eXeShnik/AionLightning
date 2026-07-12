// Port of Java data/scripts/system/handlers/quest/sarpan/_10050SomethingToProve.java (zhkchi).
// Talk to Killios (205535) to start; Vareth (205581) at var 1->2; Beshmundir (205764) at var 2->3;
// kill Klaws (218663/218664) up to 3 times (var 1), which spawns an Elite Klaw (218665) to finish
// on kill; turn in at Vareth, which pokes the zone-mission-end chain for quest 10051.
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

namespace Quest.Sarpan;

public sealed class _10050SomethingToProve : QuestHandlerBase
{
    private const int QuestIdConst = 10050;
    private const int KilliosNpc   = 205535;
    private const int VarethNpc    = 205581;
    private const int BeshmundirNpc = 205764;
    private const int Klaw1Id      = 218663;
    private const int Klaw2Id      = 218664;
    private const int EliteKlawId  = 218665;
    private const int DependentZoneMissionQuestId = 10051;

    private QuestEngine? _engine;

    public _10050SomethingToProve(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        _engine = engine;
        engine.RegisterQuestNpc(KilliosNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(KilliosNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(VarethNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BeshmundirNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Klaw1Id).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Klaw2Id).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(EliteKlawId).OnKill.Add(QuestId);
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 10040, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != KilliosNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId == KilliosNpc && var == 0)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == VarethNpc && var == 1)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SELECT_ACTION_1352) return await SendQuestDialogAsync(conn, targetObjId, 1353, ct);
                if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == BeshmundirNpc && var == 2)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3) return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == VarethNpc)
        {
            if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);

            if (_engine is not null)
                await _engine.OnZoneMissionEndAsync(new QuestEnv(env.Target, player, DependentZoneMissionQuestId, env.DialogId), conn, ct);
            await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int targetId = env.TargetId;

        if (targetId == Klaw1Id || targetId == Klaw2Id)
        {
            if (entry.GetVar(1) >= 3 || entry.GetVar(0) != 3) return false;

            entry.SetVar(1, entry.GetVar(1) + 1);
            if (entry.GetVar(1) == 3 && env.Target is not null)
            {
                var pos = env.Target.Position;
                SpawnQuestNpc(pos.WorldId, pos.InstanceId, EliteKlawId, pos.X, pos.Y, pos.Z, (byte)pos.Heading);
            }
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }

        if (targetId == EliteKlawId)
        {
            if (entry.GetVar(1) < 3 || entry.GetVar(0) != 3) return false;

            entry.Status = QuestStatus.REWARD;
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }

        return false;
    }
}
