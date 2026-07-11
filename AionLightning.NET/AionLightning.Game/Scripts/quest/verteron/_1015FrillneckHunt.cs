// Port of Java data/scripts/system/handlers/quest/verteron/_1015FrillneckHunt.java (MrPoke).
// Talk to Leto (203129), kill 210126 x7 (var 1->8), talk again (movie 27), kill 210200/210201
// x12 (var 9->20, reward-flip at 20). Zone-mission-end/level-up gated on quest 1130.
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

namespace Quest.Verteron;

public sealed class _1015FrillneckHunt : QuestHandlerBase
{
    private const int QuestIdConst = 1015;
    private const int LetoNpc      = 203129;
    private const int MobNpc       = 210126;
    private static readonly int[] _giantMobs = [210200, 210201];

    public _1015FrillneckHunt(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(LetoNpc).OnTalk.Add(QuestId);
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(MobNpc).OnKill.Add(QuestId);
        foreach (int mob in _giantMobs)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1130, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var = entry.GetVar(0);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START && env.TargetId == LetoNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 0:
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                case DialogAction.QUEST_SELECT when var == 8:
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                case DialogAction.SELECT_ACTION_1012:
                    await PlayQuestMovieAsync(conn, env.Player, 27, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1012, ct);
                case DialogAction.SETPRO1 or DialogAction.SETPRO2 when var is 0 or 8:
                    entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && env.TargetId == LetoNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        int targetId = env.TargetId;

        if (targetId == MobNpc && var is >= 1 and <= 7)
        {
            await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
            return true;
        }
        if (_giantMobs.Contains(targetId) && var is >= 9 and <= 20)
        {
            if (var == 20)
                await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
            else
                await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
            return true;
        }
        return false;
    }
}
