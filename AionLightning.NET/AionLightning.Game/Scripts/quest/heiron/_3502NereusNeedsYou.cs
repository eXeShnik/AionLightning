// Port of Java data/scripts/system/handlers/quest/heiron/_3502NereusNeedsYou.java.
// Talk to Maloren (204656) to start; use the Balaur Operation Orders (730192, var0->1); kill the
// Telepathy Controller (214894, var0 1->2), then all three generators (214895/214896/214897,
// tracked independently via var1/var2/var3) in any order — the last one spawns Brigade General
// Anuhart (214904), whose death flips to REWARD; turn in at Jucleas (203752).
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

public sealed class _3502NereusNeedsYou : QuestHandlerBase
{
    private const int QuestIdConst = 3502;
    private const int MalorenNpc = 204656;
    private const int JucleasNpc = 203752;
    private const int OrdersNpc  = 730192;
    private const int TelepathyControllerNpc = 214894;
    private const int MainGeneratorNpc        = 214895;
    private const int AuxGeneratorNpc         = 214896;
    private const int EmergencyGeneratorNpc   = 214897;
    private const int GeneralNpc              = 214904;
    private const int InstanceWorldId = 300040000;

    public _3502NereusNeedsYou(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(MalorenNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(MalorenNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(JucleasNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(OrdersNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TelepathyControllerNpc).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(MainGeneratorNpc).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(AuxGeneratorNpc).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(EmergencyGeneratorNpc).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(GeneralNpc).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var entry  = player.Quests.Get(QuestId);
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == MalorenNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == OrdersNpc)
            {
                int var = entry.GetVar(0);
                if (dialog == DialogAction.USE_OBJECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == JucleasNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var1 = entry.GetVar(1);
        int var2 = entry.GetVar(2);
        int var3 = entry.GetVar(3);

        if (env.TargetId == TelepathyControllerNpc)
        {
            if (entry.GetVar(0) == 1)
                return await DefaultOnKillEventAsync(env, conn, TelepathyControllerNpc, 1, 2, ct);
            return false;
        }
        if (env.TargetId == MainGeneratorNpc && entry.GetVar(0) == 2 && var1 != 1)
        {
            entry.SetVar(1, 1);
            await UpdateQuestStatusAsync(conn, entry, ct);
            if (var2 == 1 && var3 == 1) SpawnAnuhart(env);
            return true;
        }
        if (env.TargetId == AuxGeneratorNpc && entry.GetVar(0) == 2 && var2 != 1)
        {
            entry.SetVar(2, 1);
            await UpdateQuestStatusAsync(conn, entry, ct);
            if (var1 == 1 && var3 == 1) SpawnAnuhart(env);
            return true;
        }
        if (env.TargetId == EmergencyGeneratorNpc && entry.GetVar(0) == 2 && var3 != 1)
        {
            entry.SetVar(3, 1);
            await UpdateQuestStatusAsync(conn, entry, ct);
            if (var1 == 1 && var2 == 1) SpawnAnuhart(env);
            return true;
        }
        if (env.TargetId == GeneralNpc && entry.GetVar(0) == 2 && var1 == 1 && var2 == 1 && var3 == 1)
            return await DefaultOnKillEventAsync(env, conn, GeneralNpc, 2, reward: true, ct);

        return false;
    }

    private void SpawnAnuhart(QuestEnv env)
        => SpawnQuestNpc(InstanceWorldId, env.Player.Position.InstanceId, GeneralNpc, 275.34537f, 323.02072f, 130.9302f, 52);
}
