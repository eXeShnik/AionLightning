// Port of Java data/scripts/system/handlers/quest/theobomos/_3006TheShugoFugitive.java.
// Talk to Metatron (798132) to start; Gossip (798146) advances var 0->1 (talking to him again with
// QUEST_SELECT while var != 0 also advances it - reproduced Java fallthrough, no break after the
// var==0 check); interacting with the fugitive marker (700339) at var 1 plays movie 361, which
// flips the quest straight to REWARD; turn in at Metatron.
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

namespace Quest.Theobomos;

public sealed class _3006TheShugoFugitive : QuestHandlerBase
{
    private const int QuestIdConst = 3006;
    private const int MetatronNpc  = 798132;
    private const int GossipNpc    = 798146;
    private const int FugitiveNpc  = 700339;

    public _3006TheShugoFugitive(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(MetatronNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(MetatronNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GossipNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FugitiveNpc).OnTalk.Add(QuestId);
        engine.RegisterOnQuestMovieEnd(361, QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != MetatronNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == GossipNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO1)
                {
                    entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
            }
            else if (targetId == FugitiveNpc && var == 1)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SELECT_ACTION_1694)
                {
                    await PlayQuestMovieAsync(conn, player, 361, ct);
                    return true;
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == MetatronNpc)
            {
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        if (movieId != 361) return false;

        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        entry.SetVar(0, entry.GetVar(0) + 1);
        entry.Status = QuestStatus.REWARD;
        await UpdateQuestStatusAsync(conn, entry, ct);
        return await SendQuestDialogAsync(conn, 0, 1353, ct);
    }
}
