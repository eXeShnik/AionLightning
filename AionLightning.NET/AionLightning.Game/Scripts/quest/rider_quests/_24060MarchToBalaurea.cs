// Port of Java data/scripts/system/handlers/quest/rider_quests/_24060MarchToBalaurea.java (pralinka).
// Hub chain: Vidar (204052, var0 0->1) -> Agehia (798800, 1->2) -> Tigrina (798409, 2->3) ->
// Richelle (799225, 3->4) -> Merhen (799364, 4->5) -> Hogidin (799365, var0 5 -> REWARD); turn in at
// Valetta (799226). Asmodian-only: entering Katalam (worldId 220070000) at var0==3 plays movie 551,
// whose end (Asmodian race gate) re-broadcasts the current quest state.
// Skip vs Java: Tigrina's SETPRO3 branch called TeleportService2.teleportTo(player, 220070000, ...)
// to drop the player into Katalam - no TeleportService exists in this port; the var transition
// (2->3) is kept so the quest stays completable without the relocation.
// Java bug fixed: every dialog switch here (Vidar/Agehia/Tigrina/Richelle/Merhen/Hogidin) had no
// break after its QUEST_SELECT case, so talking with the wrong var fell through into the next
// case's body. For the plain defaultCloseDialog bodies this was harmless (self-guarded by their own
// step check), but Tigrina's SETPRO3 body ran the teleport call unconditionally before that guard -
// ported with an explicit dialog check instead of a physical fallthrough so the (now-omitted)
// relocation only would have fired on an actual SETPRO3 dialog.
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

namespace Quest.RiderQuests;

public sealed class _24060MarchToBalaurea : QuestHandlerBase
{
    private const int QuestIdConst = 24060;
    private const int VidarNpc    = 204052;
    private const int AgehiaNpc   = 798800;
    private const int TigrinaNpc  = 798409;
    private const int RichelleNpc = 799225;
    private const int MerhenNpc   = 799364;
    private const int HogidinNpc  = 799365;
    private const int ValettaNpc  = 799226;
    private const int KatalamWorldId = 220070000;

    public _24060MarchToBalaurea(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterOnQuestMovieEnd(551, QuestId);
        foreach (int npc in new[] { VidarNpc, AgehiaNpc, TigrinaNpc, RichelleNpc, MerhenNpc, HogidinNpc, ValettaNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, ct);

    public override async ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        if (movieId != 551) return false;
        var player = env.Player;
        if (player.Race != Race.ASMODIANS) return false;

        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) != 3) return false;
        if (player.Position.WorldId != KatalamWorldId) return false;

        await PlayQuestMovieAsync(conn, player, 551, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        int var0        = entry.GetVar(0);
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == VidarNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var0 == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == AgehiaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var0 == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == TigrinaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var0 == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3) return await DefaultCloseDialogAsync(env, conn, 2, 3, ct); // relocation skipped, see header
                return false;
            }
            if (targetId == RichelleNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var0 == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO4) return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                return false;
            }
            if (targetId == MerhenNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var0 == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO5) return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                return false;
            }
            if (targetId == HogidinNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var0 == 5) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (dialog == DialogAction.SET_SUCCEED) return await DefaultCloseDialogAsync(env, conn, 5, 5, reward: true, sameNpc: false, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == ValettaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 2802, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }
}
