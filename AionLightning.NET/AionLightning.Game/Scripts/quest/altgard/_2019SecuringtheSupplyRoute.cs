// Port of Java data/scripts/system/handlers/quest/altgard/_2019SecuringtheSupplyRoute.java
// (MrPoke). Talk to NPC 798033, kill mobs (210492/210493, var 1->4), receive a supply crate item,
// hand it to 203673. Zone-mission chain, level-up gated.
using System.Collections.Generic;
using System.Linq;
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

namespace Quest.Altgard;

public sealed class _2019SecuringtheSupplyRoute : QuestHandlerBase
{
    private const int QuestIdConst = 2019;
    private const int CheckpointNpc = 798033;
    private const int TurnInNpc     = 203673;
    private const int CrateItemId   = 182203024;

    private static readonly int[] _mobs = [210492, 210493];

    private readonly IItemDao _itemDao;

    public _2019SecuringtheSupplyRoute(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(CheckpointNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        foreach (int mob in _mobs)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 2200, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;
        int var = entry.GetVar(0);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            switch (targetId)
            {
                case CheckpointNpc:
                    switch (dialog)
                    {
                        case DialogAction.QUEST_SELECT when var == 0:
                            return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                        case DialogAction.QUEST_SELECT when var == 4:
                            return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                        case DialogAction.SETPRO1:
                            return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                        case DialogAction.SETPRO2:
                            return await DefaultCloseDialogAsync(env, conn, _itemDao, 4, 5, reward: false, sameNpc: false,
                                giveItemId: CrateItemId, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                        default:
                            return false;
                    }
                case TurnInNpc:
                    if (dialog == DialogAction.QUEST_SELECT && var == 5)
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD && var == 5)
                    {
                        await RemoveQuestItemAsync(env.Player, conn, _itemDao, CrateItemId, 1, ct);
                        await ChangeQuestStepAsync(conn, entry, 0, 5, toReward: true, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                    }
                    return false;
                default:
                    return false;
            }
        }
        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);
        return false;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, (IReadOnlyCollection<int>)_mobs, 1, 4, ct);
}
