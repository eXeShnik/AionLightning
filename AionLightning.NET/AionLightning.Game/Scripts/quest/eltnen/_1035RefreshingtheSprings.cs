// Port of Java data/scripts/system/handlers/quest/eltnen/_1035RefreshingtheSprings.java (Rhys2002).
// Zone-mission quest, part of the Kaidan Fortress chain (1300): talk to Gaia (203917) to advance
// var 0->1; Ophelos (203992) sends the player to insert a Life Bead into the Laquepin Life Stone
// (700158, var 2->3); Castor (203965) and Corybantes (203968) advance var 4->5->6; Heratos (203987)
// hands out a second bead, starts a 180s timer and the "insert within 3 minutes" movie (31) at the
// Desert Life Stone (700160); Sirink (203934) hands out a third bead for the Temple Life Stone
// (700159, var 10->11) and flips to REWARD; report back to Gaia.
// Skip vs Java: three TeleportService2.teleportTo calls (Ophelos SETPRO3, Corybantes SETPRO5,
// Heratos SETPRO7) relocate the player mid-chain - no TeleportService2 exists in this port (same
// precedent as quest/eltnen/_1430ATeleportationExperiment.cs). The var/status transitions are kept
// so the quest stays completable without the teleport.
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

namespace Quest.Eltnen;

public sealed class _1035RefreshingtheSprings : QuestHandlerBase
{
    private const int QuestIdConst = 1035;
    private const int GaiaNpc            = 203917;
    private const int OphelosNpc         = 203992;
    private const int LaquepinLifeStone  = 700158;
    private const int CastorNpc          = 203965;
    private const int CorybantesNpc      = 203968;
    private const int HeratosNpc         = 203987;
    private const int DesertLifeStone    = 700160;
    private const int SirinkNpc          = 203934;
    private const int TempleLifeStone    = 700159;

    private const int LaquepinBeadItem = 182201014;
    private const int DesertBeadItem   = 182201024;
    private const int TempleBeadItem   = 182201025;

    private readonly IItemDao _itemDao;

    public _1035RefreshingtheSprings(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnQuestTimerEnd(QuestId);
        engine.RegisterOnQuestMovieEnd(31, QuestId);
        foreach (int npc in new[] { GaiaNpc, OphelosNpc, LaquepinLifeStone, CastorNpc, CorybantesNpc, HeratosNpc, DesertLifeStone, SirinkNpc, TempleLifeStone })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1300, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var         = entry.GetVar(0);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != GaiaNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4080, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        if (targetId == GaiaNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 0:
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                case DialogAction.SETPRO1:
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                case DialogAction.SETPRO2:
                    return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                default:
                    return false;
            }
        }

        if (targetId == OphelosNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 1:
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                case DialogAction.QUEST_SELECT when var == 3:
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                case DialogAction.SETPRO2:
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                case DialogAction.SETPRO3:
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                default:
                    return false;
            }
        }

        if (targetId == LaquepinLifeStone && var == 2)
        {
            if (dialog == DialogAction.USE_OBJECT && (player.Inventory.FindByItemId(LaquepinBeadItem)?.Count ?? 0) == 1)
                return await UseQuestObjectAsync(env, conn, 2, 3, false, 0, 0, 0, LaquepinBeadItem, 1, 0, false, _itemDao, ct);
            return false;
        }

        if (targetId == CastorNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 4:
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                case DialogAction.SETPRO4:
                    return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                default:
                    return false;
            }
        }

        if (targetId == CorybantesNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 5:
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                case DialogAction.SETPRO5:
                    return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
                default:
                    return false;
            }
        }

        if (targetId == HeratosNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 6:
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                case DialogAction.QUEST_SELECT when var == 7:
                    return await SendQuestDialogAsync(conn, targetObjId, 2887, ct);
                case DialogAction.QUEST_SELECT when var == 8:
                    return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                case DialogAction.SETPRO6:
                    StartQuestTimer(env, conn, 180);
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 6, 7, reward: false, sameNpc: false,
                        giveItemId: DesertBeadItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                case DialogAction.FINISH_DIALOG:
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                case DialogAction.SETPRO7:
                {
                    if (entry.GetVar(0) != 7) return false;
                    await ChangeQuestStepAsync(conn, entry, 0, 8, toReward: false, ct);
                    return true;
                }
                default:
                    return false;
            }
        }

        if (targetId == DesertLifeStone && var == 7)
        {
            if (dialog == DialogAction.USE_OBJECT && (player.Inventory.FindByItemId(DesertBeadItem)?.Count ?? 0) == 1)
            {
                await PlayQuestMovieAsync(conn, player, 31, ct);
                return true;
            }
            return false;
        }

        if (targetId == SirinkNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 9:
                    return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                case DialogAction.QUEST_SELECT when var == 11:
                    return await SendQuestDialogAsync(conn, targetObjId, 3739, ct);
                case DialogAction.SETPRO8 when var == 9:
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 9, 10, reward: false, sameNpc: false,
                        giveItemId: TempleBeadItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                case DialogAction.SETPRO8 when var == 11:
                    return await DefaultCloseDialogAsync(env, conn, 11, 11, reward: true, sameNpc: false, ct);
                default:
                    return false;
            }
        }

        if (targetId == TempleLifeStone && var == 10)
        {
            if (dialog == DialogAction.USE_OBJECT && (player.Inventory.FindByItemId(TempleBeadItem)?.Count ?? 0) >= 1)
                return await UseQuestObjectAsync(env, conn, 10, 11, false, 0, 0, 0, TempleBeadItem, 1, 0, false, _itemDao, ct);
            return false;
        }

        return false;
    }

    public override async ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        if (movieId != 31) return false;

        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var == 7)
            await ChangeQuestStepAsync(conn, entry, 0, 8, toReward: false, ct);
        else if (var == 6) // If timer stopped before movie ends
            await ChangeQuestStepAsync(conn, entry, 0, 8, toReward: false, ct);

        await RemoveQuestItemAsync(env.Player, conn, _itemDao, DesertBeadItem, 1, ct);
        return true;
    }

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 7) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 6, toReward: false, ct);
        return true;
    }
}
