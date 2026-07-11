// Port of Java data/scripts/system/handlers/quest/poeta/_1001TheKerubThreat.java (author MrPoke).
// Campaign: kill 5 kerubs (var 1->6), report, collect 3x Odium Sample (182200001), turn in.
// No skips — full parity.
using System.Linq;
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

namespace Quest.Poeta;

public sealed class _1001TheKerubThreat : QuestHandlerBase
{
    private const int QuestIdConst  = 1001;
    private const int KerubNpcId    = 210670;
    private const int TalkNpcId     = 203071;
    private const int EndNpcId      = 203067;
    private const int SampleItemId  = 182200001;

    private readonly IItemDao _itemDao;

    public _1001TheKerubThreat(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(KerubNpcId).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(TalkNpcId).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EndNpcId).OnTalk.Add(QuestId);
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1100, isZoneMission: true, ct);

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.TargetId != KerubNpcId) return false;

        int var = entry.GetVar(0);
        if (var is > 0 and < 6)
        {
            entry.SetVar(0, var + 1);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var         = entry.GetVar(0);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START && targetId == TalkNpcId)
        {
            switch (DialogActionLookup.FromId(env.DialogId))
            {
                case DialogAction.SELECT_ACTION_1012:
                    await PlayQuestMovieAsync(conn, player, 15, ct);
                    return false;

                case DialogAction.QUEST_SELECT:
                    return var switch
                    {
                        0 => await SendQuestDialogAsync(conn, targetObjId, 1011, ct),
                        6 => await SendQuestDialogAsync(conn, targetObjId, 1352, ct),
                        7 => await SendQuestDialogAsync(conn, targetObjId, 1693, ct),
                        _ => false,
                    };

                case DialogAction.SETPRO3 or DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                    if (var == 7)
                    {
                        long itemCount = player.Inventory.FindByItemId(SampleItemId)?.Count ?? 0;
                        if (itemCount >= 3)
                        {
                            if (env.DialogId == (int)DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                                return await SendQuestDialogAsync(conn, targetObjId, 1694, ct);

                            await RemoveQuestItemAsync(player, conn, _itemDao, SampleItemId, itemCount, ct);
                            entry.SetVar(0, var + 1);
                            entry.Status = QuestStatus.REWARD;
                            await UpdateQuestStatusAsync(conn, entry, ct);
                            await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                            return true;
                        }
                        return await SendQuestDialogAsync(conn, targetObjId, 1779, ct);
                    }
                    return true;

                case DialogAction.SETPRO1 or DialogAction.SETPRO2:
                    if (var is 0 or 6)
                    {
                        entry.SetVar(0, var + 1);
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    }
                    return true;

                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == EndNpcId)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
