// Port of Java data/scripts/system/handlers/quest/ishalgen/_2007WheresRaeThisTime.java.
// Ulgorn (203516, var0->1) -> Nobekk (203519, var1->2) -> Derot (203539, movie 55, var2->3) ->
// Nalto (203552, var3->4) -> Rae (203554, var4->5) -> destroy 3 power generators
// (700085/700086/700087, var5->6->7->8, movie 56 on the last one) -> Rae turn-in (var8 -> REWARD)
// -> Ulgorn grants the reward (movie 58).
//
// Skip vs Java: Rae's SETPRO6 (var==8) turn-in teleports the player back to Ishalgen
// (220010000) via TeleportService2, which isn't ported; the status still flips to REWARD so the
// quest completes normally, the player just isn't relocated.
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

public sealed class _2007WheresRaeThisTime : QuestHandlerBase
{
    private const int QuestIdConst = 2007;
    private const int UlgornNpc    = 203516;
    private const int NobekkNpc    = 203519;
    private const int DerotNpc     = 203539;
    private const int NaltoNpc     = 203552;
    private const int RaeNpc       = 203554;
    private const int GreenGenObj  = 700085;
    private const int BlueGenObj   = 700086;
    private const int VioletGenObj = 700087;

    private static readonly int[] _talkNpcs = [UlgornNpc, NobekkNpc, DerotNpc, NaltoNpc, RaeNpc, GreenGenObj, BlueGenObj, VioletGenObj];
    private static readonly int[] _zoneMissionPrecedingQuests = [2006, 2005, 2004, 2003, 2002, 2001];
    private static readonly int[] _levelUpPrecedingQuests = [2100, 2006, 2005, 2004, 2003, 2002, 2001];

    public _2007WheresRaeThisTime(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int id in _talkNpcs)
            engine.RegisterQuestNpc(id).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, _zoneMissionPrecedingQuests, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, _levelUpPrecedingQuests, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var         = entry.GetVar(0);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            switch (targetId)
            {
                case UlgornNpc:
                    switch (dialog)
                    {
                        case DialogAction.QUEST_SELECT:
                            return var == 0 ? await SendQuestDialogAsync(conn, targetObjId, 1011, ct) : false;
                        case DialogAction.SETPRO1:
                            if (var != 0) return false;
                            entry.SetVar(0, 1);
                            await UpdateQuestStatusAsync(conn, entry, ct);
                            return await CloseDialogWindowAsync(conn, targetObjId, ct);
                        default:
                            return false;
                    }

                case NobekkNpc:
                    switch (dialog)
                    {
                        case DialogAction.QUEST_SELECT:
                            return var == 1 ? await SendQuestDialogAsync(conn, targetObjId, 1352, ct) : false;
                        case DialogAction.SETPRO2:
                            if (var != 1) return false;
                            entry.SetVar(0, 2);
                            await UpdateQuestStatusAsync(conn, entry, ct);
                            return await CloseDialogWindowAsync(conn, targetObjId, ct);
                        default:
                            return false;
                    }

                case DerotNpc:
                    switch (dialog)
                    {
                        case DialogAction.QUEST_SELECT:
                            return var == 2 ? await SendQuestDialogAsync(conn, targetObjId, 1693, ct) : false;
                        case DialogAction.SELECT_ACTION_1694:
                            await PlayQuestMovieAsync(conn, player, 55, ct);
                            return false;
                        case DialogAction.SETPRO3:
                            if (var != 2) return false;
                            entry.SetVar(0, 3);
                            await UpdateQuestStatusAsync(conn, entry, ct);
                            return await CloseDialogWindowAsync(conn, targetObjId, ct);
                        default:
                            return false;
                    }

                case NaltoNpc:
                    switch (dialog)
                    {
                        case DialogAction.QUEST_SELECT:
                            return var == 3 ? await SendQuestDialogAsync(conn, targetObjId, 2034, ct) : false;
                        case DialogAction.SETPRO4:
                            if (var != 3) return false;
                            entry.SetVar(0, 4);
                            await UpdateQuestStatusAsync(conn, entry, ct);
                            return await CloseDialogWindowAsync(conn, targetObjId, ct);
                        default:
                            return false;
                    }

                case RaeNpc:
                    switch (dialog)
                    {
                        case DialogAction.QUEST_SELECT:
                            return var switch
                            {
                                4 => await SendQuestDialogAsync(conn, targetObjId, 2375, ct),
                                8 => await SendQuestDialogAsync(conn, targetObjId, 2716, ct),
                                _ => false,
                            };
                        case DialogAction.SETPRO5:
                            if (var != 4) return false;
                            entry.SetVar(0, 5);
                            await UpdateQuestStatusAsync(conn, entry, ct);
                            return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                        case DialogAction.SETPRO6:
                            if (var != 8) return false;
                            entry.Status = QuestStatus.REWARD;
                            await UpdateQuestStatusAsync(conn, entry, ct);
                            // Skip: Java teleports the player back to 220010000 here (TeleportService2 not ported) — see header.
                            return true;
                        default:
                            return false;
                    }

                case GreenGenObj when var == 5:
                    Destroy(6, env, conn);
                    return false;
                case BlueGenObj when var == 6:
                    Destroy(7, env, conn);
                    return false;
                case VioletGenObj when var == 7:
                    Destroy(8, env, conn);
                    return false;

                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == UlgornNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
            {
                await PlayQuestMovieAsync(conn, player, 58, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    // Java's private destroy(): 100ms deferred, only applies while the player still targets the object.
    private void Destroy(int nextVar, QuestEnv env, GsClientConnection conn)
    {
        int targetObjectId = env.Target?.ObjectId ?? 0;
        var player = env.Player;

        _ = Task.Run(async () =>
        {
            await Task.Delay(100);
            if (player.Target?.ObjectId != targetObjectId) return;
            var entry = player.Quests.Get(QuestId);
            if (entry is null) return;

            entry.SetVar(0, nextVar);
            if (nextVar == 8)
                await PlayQuestMovieAsync(conn, player, 56, CancellationToken.None);

            await UpdateQuestStatusAsync(conn, entry, CancellationToken.None);
        });
    }
}
