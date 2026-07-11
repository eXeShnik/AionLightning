// Port of Java data/scripts/system/handlers/quest/ishalgen/_2004ACharmedCube.java.
// Derot (203539) start + collect; use the Tombstone (700047) to spawn a mob (211755); kill
// mobs (210402/210403, var 3->6); hand the charm to Munin (203550); turn in at Derot.
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

namespace Quest.Ishalgen;

public sealed class _2004ACharmedCube : QuestHandlerBase
{
    private const int QuestIdConst = 2004;
    private const int DerotNpc     = 203539;
    private const int TombstoneObj = 700047;
    private const int MuninNpc     = 203550;
    private const int CharmItemId  = 182203005;
    private static readonly int[] _mobs = [210402, 210403];

    private readonly IItemDao _itemDao;

    public _2004ACharmedCube(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(DerotNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TombstoneObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MuninNpc).OnTalk.Add(QuestId);
        foreach (int mob in _mobs)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 2100, isZoneMission: true, ct);

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
            switch (targetId)
            {
                case DerotNpc:
                    switch (dialog)
                    {
                        case DialogAction.QUEST_SELECT:
                            if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                            if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                            return false;
                        case DialogAction.SETPRO1:
                            return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                        case DialogAction.SETPRO2:
                            await GiveQuestItemAsync(player, conn, _itemDao, CharmItemId, 1, ct);
                            return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                        case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                            return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 2, false, 1438, 1353, ct);
                        case DialogAction.FINISH_DIALOG:
                            return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                        default:
                            return false;
                    }
                case TombstoneObj:
                    if (var == 1 && dialog == DialogAction.USE_OBJECT && env.Target is not null)
                    {
                        var pos = env.Target.Position;
                        SpawnQuestNpc(pos.WorldId, pos.InstanceId, 211755, pos.X, pos.Y, pos.Z, (byte)pos.Heading);
                        return true;
                    }
                    return false;
                case MuninNpc:
                    switch (dialog)
                    {
                        case DialogAction.QUEST_SELECT:
                            if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                            if (var == 6) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                            return false;
                        case DialogAction.SETPRO3:
                            return await DefaultCloseDialogAsync(env, conn, _itemDao, 2, 3, reward: false, sameNpc: false,
                                giveItemId: 0, giveItemCount: 0, removeItemId: CharmItemId, removeItemCount: 1, ct);
                        case DialogAction.SETPRO4:
                            return await DefaultCloseDialogAsync(env, conn, 6, 6, reward: true, sameNpc: false, ct);
                        default:
                            return false;
                    }
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == DerotNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, (IReadOnlyCollection<int>)_mobs, 3, 6, ct);
}
