// Port of Java data/scripts/system/handlers/quest/heiron/_1051TheRuinsofRoah.java.
// Talk to Sarantus (204501) to start; advance through Ibelia (204582), the Engraved Stone Tablet
// object (700217, gives quest item 182201601), Ipikio (203882) and Calon (278503, collect-check
// for the final reward); turn in at Sarantus. Zone-mission-end/level-up gated on quest 1500.
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

namespace Quest.Heiron;

public sealed class _1051TheRuinsofRoah : QuestHandlerBase
{
    private const int QuestIdConst = 1051;
    private const int SarantusNpc  = 204501;
    private const int IbeliaNpc    = 204582;
    private const int IpikioNpc    = 203882;
    private const int CalonNpc     = 278503;
    private const int StonePlateNpc = 700303;
    private const int StoneTabletNpc = 700217;
    private const int StoneTabletItem = 182201601;

    private readonly IItemDao _itemDao;

    public _1051TheRuinsofRoah(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(SarantusNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(SarantusNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(IbeliaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(IpikioNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(CalonNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StonePlateNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StoneTabletNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 1500, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);
        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == SarantusNpc)
                return await SendQuestEndDialogAsync(env, conn, ct);
            return false;
        }
        if (entry.Status != QuestStatus.START) return false;

        if (targetId == SarantusNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO1 && var == 0)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            if (dialog == DialogAction.SETPRO5 && var == 4)
                return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
        }
        else if (targetId == IbeliaNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO2 && var == 1)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            if (dialog == DialogAction.SETPRO4 && var == 3)
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 3, 4, reward: false, sameNpc: false,
                    giveItemId: 0, giveItemCount: 0, removeItemId: StoneTabletItem, removeItemCount: 1, ct);
        }
        else if (targetId == IpikioNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 5)
                return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
            if (dialog == DialogAction.SETPRO6 && var == 5)
                return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
        }
        else if (targetId == CalonNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 6) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                if (var == 7) return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                return false;
            }
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 7, 7, true, 10000, 10001, ct);
            if (dialog == DialogAction.SETPRO7 && var == 6)
                return await DefaultCloseDialogAsync(env, conn, 6, 7, ct);
        }
        else if (targetId == StoneTabletNpc && var == 2)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SETPRO3)
                return await UseQuestObjectAsync(env, conn, 2, 3, false, 0, StoneTabletItem, 1, 0, 0, 0, false, _itemDao, ct);
        }
        else if (targetId == StonePlateNpc && var == 7)
        {
            if (dialog == DialogAction.USE_OBJECT) return true;
        }
        return false;
    }
}
