// Port of Java data/scripts/system/handlers/quest/rider_quests/_14060TheLandOfIde.java (pralinka).
// Zone-mission sub-quest of 14050(ish) chain: talk to Fasimedes (203700, var0 0->1), Eremitia
// (798600, var0 1->2), Sibylle (798408, var0 2->3, movie 501), Outremus (798926, var0 3->4), Nydrea
// (799053, var0 4->REWARD via raw dialog id 10004), turn in at Versetti (798927). Re-plays movie 501
// on re-entering world 210050000 while still at var0==7 (leftover Java quirk kept as-is).
// Skip vs Java: Sibylle's SETPRO3 case teleports the player to world 210050000 via TeleportService2
// (not ported in this port) - the movie play, var transition and dialog stay, only the relocation is
// dropped (same precedent as quest/rider_quests/_14024AKrallingSuspicion.cs).
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

namespace Quest.RiderQuests;

public sealed class _14060TheLandOfIde : QuestHandlerBase
{
    private const int QuestIdConst = 14060;
    private const int FasimedesNpc = 203700;
    private const int EremitiaNpc  = 798600;
    private const int SibylleNpc   = 798408;
    private const int OutremusNpc  = 798926;
    private const int NydreaNpc    = 799053;
    private const int VersettiNpc  = 798927;
    private const int TursinWorldId = 210050000;

    public _14060TheLandOfIde(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(FasimedesNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EremitiaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SibylleNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(OutremusNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(NydreaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(VersettiNpc).OnTalk.Add(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 7) return false;
        if (player.Position.WorldId != TursinWorldId) return false;

        await PlayQuestMovieAsync(conn, player, 501, ct);
        return true;
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);
        int var         = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == FasimedesNpc && var == 0)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (targetId == EremitiaNpc && var == 1)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (targetId == SibylleNpc && var == 2)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
                    await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    await PlayQuestMovieAsync(conn, player, 501, ct);
                    // Skip: Java teleports the player to world 210050000 here via TeleportService2 (not ported).
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (targetId == OutremusNpc && var == 3)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO4) return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (targetId == NydreaNpc && var == 4)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (env.DialogId == 10004)
                {
                    await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == VersettiNpc)
            {
                if (env.DialogId == -3) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }
}
