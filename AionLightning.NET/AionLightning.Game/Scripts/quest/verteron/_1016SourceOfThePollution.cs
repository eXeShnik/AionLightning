// Port of Java data/scripts/system/handlers/quest/verteron/_1016SourceOfThePollution.java
// (Rhys2002, Nephis, reworked vlog). A relay chain across Geolus/Lepios/Dimos/Jumentis/Quintus/
// Hygea/Kato, spawning Kato (203195) on the last mob kill, turning in at Spatalos (203098).
// Zone-mission-end/level-up gated on quest 1130.
// Java bug: Geolus's case QUEST_SELECT falls through (missing break) into SELECT_ACTION_3400's
// body when var doesn't match 0/2/7/8, and case SETPRO8 falls into FINISH_DIALOG's body when var
// isn't 7/8 — both would misfire regardless of the actual dialog id sent. Ported here as
// independent per-case branches instead. The other NPCs' QUEST_SELECT->SETPROx fallthroughs are
// harmless (the SETPROx body re-checks the same var it guards on), so those are unaffected.
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

namespace Quest.Verteron;

public sealed class _1016SourceOfThePollution : QuestHandlerBase
{
    private const int QuestIdConst = 1016;
    private const int GeolusNpc    = 203149;
    private const int LepiosNpc    = 203148;
    private const int DimosNpc     = 203832;
    private const int JumentisNpc  = 203705;
    private const int QuintusNpc   = 203822;
    private const int HygeaNpc     = 203761;
    private const int SpatalosNpc  = 203098;
    private const int KatoNpc      = 203195;
    private const int MobNpc       = 210318;

    private static readonly int[] _npcs =
        [GeolusNpc, LepiosNpc, DimosNpc, JumentisNpc, QuintusNpc, HygeaNpc, SpatalosNpc, KatoNpc];

    private readonly IItemDao _itemDao;

    public _1016SourceOfThePollution(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(MobNpc).OnKill.Add(QuestId);
        foreach (int npc in _npcs)
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1130, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (env.TargetId != SpatalosNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, 182200016, 1, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 4080, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        if (entry.Status != QuestStatus.START) return false;
        int var = entry.GetVar(0);

        if (env.TargetId == GeolusNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 0:
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                case DialogAction.QUEST_SELECT when var == 2:
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                case DialogAction.QUEST_SELECT when var == 7:
                    return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                case DialogAction.QUEST_SELECT when var == 8:
                    return (player.Inventory.FindByItemId(182200015)?.Count ?? 0) < 2
                        ? await SendQuestDialogAsync(conn, targetObjId, 3484, ct)
                        : await SendQuestDialogAsync(conn, targetObjId, 3569, ct);
                case DialogAction.SELECT_ACTION_3400:
                    await PlayQuestMovieAsync(conn, player, 28, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 3400, ct);
                case DialogAction.SETPRO1:
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                case DialogAction.SETPRO3:
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                case DialogAction.SETPRO8 when var == 7:
                    await RemoveQuestItemAsync(player, conn, _itemDao, 182200013, 1, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, 182200014, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 7, 8,
                        reward: false, sameNpc: false, giveItemId: 182200015, giveItemCount: 2, removeItemId: 0, removeItemCount: 0, ct);
                case DialogAction.SETPRO8 when var == 8:
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 8, 8,
                        reward: false, sameNpc: false, giveItemId: 182200015, giveItemCount: 2, removeItemId: 0, removeItemCount: 0, ct);
                case DialogAction.FINISH_DIALOG:
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                default:
                    return false;
            }
        }

        if (env.TargetId == LepiosNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 1:
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                case DialogAction.SETPRO2:
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 1, 2,
                        reward: false, sameNpc: false, giveItemId: 182200017, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                default:
                    return false;
            }
        }

        if (env.TargetId == DimosNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 3:
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                case DialogAction.SETPRO4:
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 3, 4,
                        reward: false, sameNpc: false, giveItemId: 182200013, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                default:
                    return false;
            }
        }

        if (env.TargetId == JumentisNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 4:
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                case DialogAction.SETPRO5:
                    return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                default:
                    return false;
            }
        }

        if (env.TargetId == QuintusNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 5:
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                case DialogAction.SETPRO6:
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 5, 6,
                        reward: false, sameNpc: false, giveItemId: 182200018, giveItemCount: 1, removeItemId: 182200017, removeItemCount: 1, ct);
                default:
                    return false;
            }
        }

        if (env.TargetId == HygeaNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 6:
                    return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                case DialogAction.SETPRO7:
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 6, 7,
                        reward: false, sameNpc: false, giveItemId: 182200014, giveItemCount: 1, removeItemId: 182200018, removeItemCount: 1, ct);
                default:
                    return false;
            }
        }

        if (env.TargetId == KatoNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 9:
                    return await SendQuestDialogAsync(conn, targetObjId, 3739, ct);
                case DialogAction.SETPRO9 when var == 9:
                {
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, 182200016, 1, ct))
                        return await CloseDialogWindowAsync(conn, targetObjId, ct);

                    await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, 182200015, 2, ct);
                    // Java also despawns Kato via its AI controller (onDelete) — no NPC controller
                    // infra exists yet to port this; harmless, Kato just stays visible.
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                default:
                    return false;
            }
        }

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.TargetId != MobNpc || entry.GetVar(0) != 8) return false;

        if (env.Target is not null)
            SpawnQuestNpc(210030000, env.Player.Position.InstanceId, KatoNpc,
                env.Target.Position.X, env.Target.Position.Y, env.Target.Position.Z, 0);

        return await DefaultOnKillEventAsync(env, conn, MobNpc, 8, 9, ct);
    }
}
