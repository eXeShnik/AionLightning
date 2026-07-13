// Port of Java data/scripts/system/handlers/quest/rider_quests/_24012AnOminousCrop.java (pralinka).
// Zone-mission sub-quest of 24010: talk to 203605 (gives Rough Flint, var0 0->1), enter the Mumu
// Farmland zone (var0 1->2), use quest object 700096 three times (var0 2->5), collect-check +
// remove the flint at 203605 turn-in (var0 5->reward). Also registers the real onEnterZone hook for
// the zone-crossing step, distinct from the zone-mission-end poke used to (re)start this quest.
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

public sealed class _24012AnOminousCrop : QuestHandlerBase
{
    private const int QuestIdConst = 24012;
    private const int FarmerNpc = 203605;
    private const int ScarecrowNpc = 700096;
    private const int RoughFlintItem = 182215356;
    private const string FarmlandZone = "MUMU_FARMLAND_220030000";

    private readonly IItemDao _itemDao;

    public _24012AnOminousCrop(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(FarmerNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ScarecrowNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, FarmlandZone);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, precedingQuestId: 24011, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 24010, isZoneMission: true, ct);

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != FarmlandZone) return false;
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;

        // Java changeQuestStep(env, 1, 2, false): var write + status broadcast only, no dialog packet.
        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
        return true;
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

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == FarmerNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.QUEST_SELECT && var == 5)
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 0, 1, reward: false, sameNpc: false,
                        giveItemId: RoughFlintItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 5, 5, reward: true, checkOkId: 5, checkFailId: 2120, ct);
                return false;
            }
            if (targetId == ScarecrowNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                {
                    if (var >= 2 && var < 4)
                        return await UseQuestObjectAsync(env, conn, var, var + 1, reward: false, dieObject: true, ct);
                    if (var == 4)
                        return await UseQuestObjectAsync(env, conn, 4, 5, reward: false, dieObject: true, ct);
                }
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == FarmerNpc)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, RoughFlintItem, 1, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }
}
