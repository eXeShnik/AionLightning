// Port of Java data/scripts/system/handlers/quest/inggison/_10026SecretOfInggison.java.
// Steropes (799052, var0->1, plays movie 14) -> Nydrea (799053, plays movie 15 and flips to REWARD)
// -> turn-in at Nydrea via the normal SELECT_QUEST_REWARD/RewardService flow. Java's own SETPRO3
// case (manually flipping REWARD -> COMPLETE) is unreachable dead code: the moment status==REWARD,
// Java's onDialogEvent unconditionally returns sendQuestEndDialog(env) for any dialog targeting
// Nydrea *before* the switch containing SETPRO3 is even reached — so it is dropped here too. Java's
// Nydrea QUEST_SELECT case also has a second `else if (var == 1)` branch identical to (and thus
// unreachable after) the first — dropped as dead code.
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

namespace Quest.Inggison;

public sealed class _10026SecretOfInggison : QuestHandlerBase
{
    private const int QuestIdConst = 10026;
    private const int Steropes = 799052;
    private const int Nydrea   = 799053;
    private const int PrecedingQuestId = 10000;

    public _10026SecretOfInggison(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(Steropes).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Steropes).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Nydrea).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: PrecedingQuestId, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var         = entry.GetVar(0);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != Nydrea) return false;
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        if (targetId == Steropes)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1 && var == 0)
            {
                await PlayQuestMovieAsync(conn, player, 14, ct);
                entry.SetVar(0, var + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if (targetId == Nydrea)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SET_SUCCEED && var == 1)
            {
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                await PlayQuestMovieAsync(conn, player, 15, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            return false;
        }

        return false;
    }
}
