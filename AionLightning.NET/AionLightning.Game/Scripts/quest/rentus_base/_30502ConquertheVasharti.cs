// Port of Java data/scripts/system/handlers/quest/rentus_base/_30502ConquertheVasharti.java (maddison).
// Start at 799666; entering SPARRING_GROUNDS_300280000 advances var0 0->1; then a kill chain: at
// var0==1 kill 217307 & 217308 (independent sub-vars var1/var2) which together advance var0->2,
// kill 217310 (var0->3), kill 217313 (var0->5); talk to 799670 (USE_OBJECT page, SET_SUCCEED ->
// reward at var0==5); turn in at 799544.
// note: Java's canRepeat() re-entry is approximated as "no active entry".
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

namespace Quest.RentusBase;

public sealed class _30502ConquertheVasharti : QuestHandlerBase
{
    private const int QuestIdConst = 30502;
    private const int StartNpc     = 799666;
    private const int MidNpc       = 799670;
    private const int EndNpc       = 799544;
    private const string SparringZone = "SPARRING_GROUNDS_300280000";

    public _30502ConquertheVasharti(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(217307).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(217308).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(217313).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(217310).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(MidNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EndNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, SparringZone);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null) // Java: qs == null || NONE || canRepeat()
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == MidNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                    return await DefaultCloseDialogAsync(env, conn, 5, 5, reward: true, sameNpc: false, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == EndNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != SparringZone) return false;
        var player = env.Player;
        if (player is null) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is not null && entry.Status == QuestStatus.START)
        {
            if (entry.GetVar(0) == 0)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                return true;
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var = entry.GetVar(0);
        int targetId = env.TargetId;

        if (entry.Status != QuestStatus.START) return false;

        if (var == 1)
        {
            if (targetId == 217307) entry.SetVar(1, 1);
            else if (targetId == 217308) entry.SetVar(2, 1);
            await UpdateQuestStatusAsync(conn, entry, ct);
            if (entry.GetVar(1) == 1 && entry.GetVar(2) == 1)
                await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
        }
        else if (var == 2)
        {
            if (targetId == 217310)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
                await UpdateQuestStatusAsync(conn, entry, ct); // Java: redundant updateQuestStatus after changeQuestStep
            }
        }
        else if (var == 3)
        {
            if (targetId == 217313)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 5, toReward: false, ct);
                await UpdateQuestStatusAsync(conn, entry, ct); // Java: redundant updateQuestStatus after changeQuestStep
            }
        }
        return false;
    }
}
