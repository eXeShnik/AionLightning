// Port of Java data/scripts/system/handlers/quest/aturam_sky_fortress/_28303Just_an_Island.java (zhkchi).
// Asmodian mirror of _18303Making_A_Sur: same npc ids (799530/730390/700980/799531, mobs 217382/217376).
// Skip vs Java: 730390's SETPRO1 calls TeleportService2.teleportTo — no TeleportService2 exists in
// this port (matches eltnen/_1430ATeleportationExperiment.cs); the dialog transition is kept, the
// teleport itself is dropped.
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

namespace Quest.AturamSkyFortress;

public sealed class _28303Just_an_Island : QuestHandlerBase
{
    private const int QuestIdConst = 28303;
    private const int StartNpc     = 799530;
    private const int TeleportNpc  = 730390;
    private const int UseObjectNpc = 700980;
    private const int TurnInNpc    = 799531;
    private const int Mob1         = 217382;
    private const int Mob2         = 217376;

    public _28303Just_an_Island(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TeleportNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(UseObjectNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Mob1).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Mob2).OnKill.Add(QuestId);
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
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
                {
                    await PlayQuestMovieAsync(conn, player, 470, ct);
                    return await SendQuestStartDialogAsync(env, conn, ct);
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == TeleportNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1007, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    // Java bug: TeleportService2.teleportTo(player, 300240000, 158.88f, 624.42f, 901f, 20)
                    // skipped — no TeleportService2 in this port (see eltnen/_1430ATeleportationExperiment.cs).
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (targetId == UseObjectNpc)
                return await UseQuestObjectAsync(env, conn, 2, 3, reward: true, dieObject: true, ct);
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == TurnInNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var == 0)
            return await DefaultOnKillEventAsync(env, conn, Mob1, 0, 1, ct);
        return await DefaultOnKillEventAsync(env, conn, Mob2, 1, 2, ct);
    }
}
