// Port of Java data/scripts/system/handlers/quest/rentus_base/_30552DriveOutTheVasharti.java (Ritsu).
// Start at Ariana (799666); entering SPARRING_GROUNDS_300280000 advances var0 0->1; kill 217307
// (bumps counter var1 while var0==1) / 217308 (advances var0->2); entering SIELS_FORGE_300280000 at
// var0==2 advances var0->3; kill 217313 at var0==3 advances var0->5; talk to 799670 (USE_OBJECT
// page, SET_SUCCEED -> var0->6); turn in at Oreitia (799544, SELECT_QUEST_REWARD -> reward, sameNpc).
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

public sealed class _30552DriveOutTheVasharti : QuestHandlerBase
{
    private const int QuestIdConst = 30552;
    private const int ArianaNpc    = 799666;
    private const int MidNpc       = 799670;
    private const int OreitiaNpc   = 799544;
    private const string SparringZone = "SPARRING_GROUNDS_300280000";
    private const string SielsForgeZone = "SIELS_FORGE_300280000";

    private static readonly int[] MobIds = [217307, 217308, 217313];

    public _30552DriveOutTheVasharti(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(ArianaNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(ArianaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MidNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, SparringZone);
        RegisterOnEnterZone(engine, SielsForgeZone);
        foreach (int id in MobIds)
            engine.RegisterQuestNpc(id).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(OreitiaNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null) // Java: qs == null || NONE
        {
            if (targetId == ArianaNpc)
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
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                    return await DefaultCloseDialogAsync(env, conn, 5, 6, reward: false, sameNpc: false, ct);
                return false;
            }
            if (targetId == OreitiaNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await DefaultCloseDialogAsync(env, conn, 6, 6, reward: true, sameNpc: true, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == OreitiaNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is not null && entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (zoneName == SparringZone)
            {
                if (var == 0)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                    return true;
                }
            }
            else if (zoneName == SielsForgeZone)
            {
                if (var == 2)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct); // Java: setQuestVar(3) + updateQuestStatus
                    return true;
                }
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(1);
        int targetId = env.TargetId;

        // Java switch fallthrough: case 217307, when var0 != 1, has no break and falls into 217308.
        if (targetId == 217307)
        {
            if (entry.GetVar(0) == 1)
            {
                entry.SetVar(1, var + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
            // fallthrough into 217308
            entry.SetVar(0, 2);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        if (targetId == 217308)
        {
            entry.SetVar(0, 2);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        if (targetId == 217313)
        {
            if (entry.GetVar(0) == 3)
            {
                entry.SetVar(0, 5); // Java: setQuestVar(5)
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
        }
        return false;
    }
}
