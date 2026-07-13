// Port of Java data/scripts/system/handlers/quest/convent_of_marchutan/_20000VidarsCall.java
// (Nephis, mod Gigi/vlog). Asmodian Gelkmaros campaign opener: auto-starts on entering world
// 120010000; talk to Vidar (204052) to flip START->REWARD; turn in at Eremita (798800), which pokes
// the dependent Gelkmaros zone missions (20001/20026/20020-20025) to start (Java onEnterZoneMissionEnd).
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

namespace Quest.ConventOfMarchutan;

public sealed class _20000VidarsCall : QuestHandlerBase
{
    private const int QuestIdConst = 20000;
    private const int VidarNpc     = 204052;
    private const int EremitaNpc   = 798800;
    private const int StartWorldId = 120010000;

    private static readonly int[] DependentQuests = { 20001, 20026, 20020, 20021, 20022, 20023, 20024, 20025 };

    private QuestEngine _engine = null!;

    public _20000VidarsCall(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        _engine = engine;
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterQuestNpc(VidarNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EremitaNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        if (player.Position.WorldId != StartWorldId) return false;
        if (player.Quests.Get(QuestId) is not null) return false;
        return await StartMissionAsync(conn, player, QuestStatus.START, ct);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var         = entry.GetVar(0);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId != VidarNpc) return false;

            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);

            // Java switch fallthrough: QUEST_SELECT (var != 0) falls into SET_SUCCEED
            if (dialog == DialogAction.SET_SUCCEED || dialog == DialogAction.QUEST_SELECT)
            {
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != EremitaNpc) return false;

            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);

            foreach (int quest in DependentQuests)
                await _engine.OnZoneMissionEndAsync(new QuestEnv(env.Target, player, quest, env.DialogId), conn, ct);

            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
