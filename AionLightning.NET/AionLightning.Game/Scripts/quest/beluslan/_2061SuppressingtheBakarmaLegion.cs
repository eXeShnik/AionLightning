// Port of Java data/scripts/system/handlers/quest/beluslan/_2061SuppressingtheBakarmaLegion.java.
// Zone-mission closer gated on all 10 other Beluslan S/A quests. 204702 (var0->1, var2->3, movie
// 255, var9->REWARD), 278001 (var1->2, gives demolition charge 182204320), 204807 (var3->4), then
// repeatedly using device 700295 (var4..7 -> +1 each time, removing the charge at var7). Skip vs
// Java: the device use also instantly kills the player's current target creature
// (Creature.getController().onAttack(..., maxHp+1, true)) — no Creature/attack infra exposed to
// quest handlers, and it doesn't gate the quest's own var progression, so it's omitted.
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

namespace Quest.Beluslan;

public sealed class _2061SuppressingtheBakarmaLegion : QuestHandlerBase
{
    private const int QuestIdConst = 2061;
    private const int Npc702 = 204702;
    private const int Npc001 = 278001;
    private const int Npc807 = 204807;
    private const int DeviceObj = 700295;
    private const int FinalNpc = 204052;
    private const int DemolitionChargeItem = 182204320;

    private static readonly int[] _precedingQuests = [2051, 2052, 2053, 2054, 2055, 2056, 2057, 2058, 2059, 2060];

    private readonly IItemDao _itemDao;

    public _2061SuppressingtheBakarmaLegion(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        RegisterOnEnterZone(engine, "MALEK_MINE_220040000");
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(700290).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(214026).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Npc702).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc001).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc807).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(DeviceObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FinalNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, _precedingQuests, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, [2500, .. _precedingQuests], isZoneMission: true, ct);

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        if (env.TargetId == 700290) return DefaultOnKillEventAsync(env, conn, 700290, 5, 8, ct);
        if (env.TargetId == 214026) return DefaultOnKillEventAsync(env, conn, 214026, 8, 9, ct);
        return ValueTask.FromResult(false);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var = entry.GetVar(0);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == FinalNpc)
            {
                if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }
        if (entry.Status != QuestStatus.START) return false;

        if (targetId == Npc702)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (var == 9) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                return false;
            }
            if (dialog == DialogAction.SELECT_ACTION_1694)
            {
                await PlayQuestMovieAsync(conn, player, 255, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            if (dialog == DialogAction.SETPRO3) return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
            if (dialog == DialogAction.SET_SUCCEED && var == 9)
            {
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            return false;
        }
        if (targetId == Npc001)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO2)
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 1, 2, reward: false, sameNpc: false,
                    giveItemId: DemolitionChargeItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
            return false;
        }
        if (targetId == Npc807)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            if (dialog == DialogAction.SETPRO4) return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
            return false;
        }
        if (targetId == DeviceObj && var is >= 4 and < 8)
        {
            if (dialog == DialogAction.USE_OBJECT)
            {
                if (var == 7) await RemoveQuestItemAsync(player, conn, _itemDao, DemolitionChargeItem, 1, ct);
                await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
                return true;
            }
            return false;
        }
        return false;
    }
}
