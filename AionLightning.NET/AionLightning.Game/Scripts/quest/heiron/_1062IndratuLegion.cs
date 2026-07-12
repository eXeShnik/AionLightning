// Port of Java data/scripts/system/handlers/quest/heiron/_1062IndratuLegion.java.
// Talk to 204500 (var0->1), 204600 (var1->2), 204610 (var2->3); kill 700220 repeatedly (var2..12,
// each kill +1), the 12->13 kill spawns Brigadier Indratu (212588) nearby, whose death at var13
// flips to REWARD. Mission-chain quest (no NPC quest-offer dialog), gated on 1500.
// Skip vs Java: the flight-teleport emote/state broadcast on 204600's SETPRO2 and the intermediate
// "page 5" reward-picker dialog aren't ported (no flight-teleport animation infra; the reward
// service already resolves the chosen tier from the unified SELECT_QUEST_REWARD action).
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

public sealed class _1062IndratuLegion : QuestHandlerBase
{
    private const int QuestIdConst = 1062;
    private const int FirstNpc  = 204500;
    private const int SecondNpc = 204600;
    private const int ThirdNpc  = 204610;
    private const int KillNpc      = 700220;
    private const int BrigadierNpc = 212588;

    public _1062IndratuLegion(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(BrigadierNpc).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(KillNpc).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(FirstNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SecondNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ThirdNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 1500, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == FirstNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }
        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (targetId == FirstNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1 && var == 0)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
        }
        else if (targetId == SecondNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO2 && var == 1)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
        }
        else if (targetId == ThirdNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 2)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SELECT_ACTION_1694)
            {
                await PlayQuestMovieAsync(conn, player, 195, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO3 && var == 2)
                return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (env.TargetId == KillNpc)
        {
            int var = entry.GetVar(0);
            if (var > 2 && var < 12)
            {
                entry.SetVar(0, var + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
            if (var == 12)
            {
                entry.SetVar(0, var + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                if (env.Target is not null)
                    SpawnQuestNpc(env.Target.Position.WorldId, env.Target.Position.InstanceId, BrigadierNpc,
                        env.Target.Position.X, env.Target.Position.Y, env.Target.Position.Z, (byte)env.Target.Position.Heading);
                return true;
            }
        }
        else if (env.TargetId == BrigadierNpc && entry.GetVar(0) == 13)
        {
            entry.Status = QuestStatus.REWARD;
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }
}
