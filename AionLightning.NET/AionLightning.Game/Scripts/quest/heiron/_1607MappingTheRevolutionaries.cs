// Port of Java data/scripts/system/handlers/quest/heiron/_1607MappingTheRevolutionaries.java (vlog).
// Using item 182201744 starts the quest (dialog 4); talk to Kuobe (204578, var0->1); entering any of
// the four experiment-lab zones sets that lab's own tracking var to 1 (independent per-zone flags,
// var1-var4); once all four are 1, Finn (204574) shows the reward-select dialog and completes it.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Heiron;

public sealed class _1607MappingTheRevolutionaries : QuestHandlerBase
{
    private const int QuestIdConst = 1607;
    private const int KuobeNpc = 204578;
    private const int FinnNpc  = 204574;
    private const int MapItem  = 182201744;
    private const string MudthornZone = "MUDTHORN_EXPERIMENT_LAB_210040000";
    private const string RotronZone    = "ROTRON_EXPERIMENT_LAB_210040000";
    private const string PretorZone    = "PRETOR_EXPERIMENT_LAB_210040000";
    private const string PoisonZone    = "POISON_EXTRACTION_LAB_210040000";

    public _1607MappingTheRevolutionaries(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(MapItem, QuestId);
        engine.RegisterQuestNpc(KuobeNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FinnNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, MudthornZone);
        RegisterOnEnterZone(engine, RotronZone);
        RegisterOnEnterZone(engine, PretorZone);
        RegisterOnEnterZone(engine, PoisonZone);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != MapItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is not null && entry.Status != QuestStatus.NONE) return false;

        if (!await StartMissionAsync(conn, player, QuestStatus.START, ct)) return false;
        return await SendQuestDialogAsync(conn, 0, 4, ct);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            int var  = entry.GetVar(0);
            int var1 = entry.GetVar(1);
            int var2 = entry.GetVar(2);
            int var3 = entry.GetVar(3);
            int var4 = entry.GetVar(4);

            if (targetId == KuobeNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
            else if (targetId == FinnNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 1 && var1 == 1 && var2 == 1 && var3 == 1 && var4 == 1)
                        return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                    return false;
                }
                if (dialog == DialogAction.SELECT_QUEST_REWARD && var == 1)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == FinnNpc)
                return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var != 1) return false;

        // Java bug: the Pretor/Poison checks are `else if`s nested *inside* the Rotron zone's inner
        // `if (var2 == 0) {...} else if (zoneName == PRETOR) {...}` block, so they can only run when
        // zoneName already equals Rotron and var2 != 0 - a contradiction that makes them unreachable
        // dead code in the original. Ported here as four independent checks so all four labs work.
        if (zoneName == MudthornZone && entry.GetVar(1) == 0)
        {
            await ChangeQuestStepAsync(conn, entry, 1, 1, toReward: false, ct);
            return true;
        }
        if (zoneName == RotronZone && entry.GetVar(2) == 0)
        {
            await ChangeQuestStepAsync(conn, entry, 2, 1, toReward: false, ct);
            return true;
        }
        if (zoneName == PretorZone && entry.GetVar(3) == 0)
        {
            await ChangeQuestStepAsync(conn, entry, 3, 1, toReward: false, ct);
            return true;
        }
        if (zoneName == PoisonZone && entry.GetVar(4) == 0)
        {
            await ChangeQuestStepAsync(conn, entry, 4, 1, toReward: false, ct);
            return true;
        }
        return false;
    }
}
