// Hand-written quest script (Phase 5 Batch 0 golden exemplar). Port of Java
// data/scripts/system/handlers/quest/poeta/_1002RequestoftheElim.java.
//
// Multi-npc dialog var table: Ampeis (203076) starts the chain, Forest Protector Noah (730007)
// hands out/collects the evidence item (182200002) and plays a short cutscene (movie 20), the
// Sleeping Elder (730010) is a "use object" step, Daminu (730008) advances the tail end, and Kalio
// (203067) pays out the reward once the quest reaches REWARD.
//
// NOT PORTED (documented deviation — this port has neither service, so the branch is either
// dropped or collapsed onto an already-reachable path instead of leaving a dead end):
//   - Java's SETPRO5 case (Daminu, var 13) teleports the player into instance zone 310010000 via
//     InstanceService.getNextAvailableInstance + TeleportService2.teleportTo, sets var 13->20, and
//     Java's onEnterWorldEvent override then sends SM_ASCENSION_MORPH while inside that zone and
//     corrects var 20->13 once the player leaves it. Neither InstanceService, TeleportService2,
//     nor SM_ASCENSION_MORPH exist in this port yet, so this script's SETPRO5 case instead
//     transitions var 13->14 directly (the same target var Java's SETPRO6/var-20-return path
//     would eventually reach) and OnEnterWorldAsync is not overridden at all.
//   - Java's var==20 dialog case at Belpartan (205000) — a 43s scheduled flight-teleport back to
//     Verteron via ThreadPoolManager — is unreachable once var never becomes 20, so it is omitted;
//     NPC 205000 is still registered for OnTalk parity with Java's npc list even though no case
//     handles it.
//   - The Sleeping Elder's Java `getController().scheduleRespawn()`/`.onDelete()` calls (a
//     despawn/respawn visual on each object-use) are skipped — this port has no Npc AI/controller
//     subsystem yet — but the underlying var transition (2->4->5) is still ported via
//     UseQuestObjectAsync so the quest remains fully completable end-to-end.
//   - Java's onCanAct override (gates whether the Sleeping Elder can even be interacted with
//     outside var 2/4) has no equivalent hook in this port's IQuestHandler and is omitted; the
//     var-equality check inside UseQuestObjectAsync already prevents out-of-order progress.
//   - Java's two `useQuestObject` calls for var 2 and var 4 differ in whether they `return` the
//     call's result (var==2 silently discards it, a likely upstream bug); this port returns both
//     uniformly for a properly-closed dialog session in both cases.
//
// Script-authoring convention (see QuestEngineHostedService.LoadHandWrittenScripts): subclass
// QuestHandlerBase, hardcode the quest id as a compile-time constant passed to the base
// constructor, and expose exactly one public constructor shaped
// (IDataManager, IQuestDao, QuestRewardService, IItemDao) — the batch-compile host resolves and
// invokes that fixed constructor by reflection.
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

namespace Quest.Poeta;

public sealed class _1002RequestoftheElim : QuestHandlerBase
{
    private const int QuestIdConst = 1002;
    private const int EvidenceItemId = 182200002;

    private const int NpcAmpeis        = 203076;
    private const int NpcForestProtect = 730007;
    private const int NpcSleepingElder = 730010;
    private const int NpcDaminu        = 730008;
    private const int NpcBelpartan     = 205000; // registered for parity; no reachable dialog case (see file header)
    private const int NpcKalio         = 203067;

    private readonly IItemDao _itemDao;

    public _1002RequestoftheElim(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        int[] npcs = { NpcAmpeis, NpcForestProtect, NpcSleepingElder, NpcDaminu, NpcBelpartan, NpcKalio };
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npc in npcs)
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1100, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var         = entry.GetVar(0);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != NpcKalio) return false;
            return dialog == DialogAction.USE_OBJECT
                ? await SendQuestDialogAsync(conn, targetObjId, 2716, ct)
                : await SendQuestEndDialogAsync(env, conn, ct);
        }

        if (entry.Status != QuestStatus.START) return false;

        switch (targetId)
        {
            case NpcAmpeis:
                return dialog switch
                {
                    DialogAction.QUEST_SELECT when var == 0 => await SendQuestDialogAsync(conn, targetObjId, 1011, ct),
                    DialogAction.SETPRO1                    => await DefaultCloseDialogAsync(env, conn, 0, 1, ct),
                    _                                        => false,
                };

            case NpcForestProtect:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        return var switch
                        {
                            1  => await SendQuestDialogAsync(conn, targetObjId, 1352, ct),
                            5  => await SendQuestDialogAsync(conn, targetObjId, 1693, ct),
                            6  => await SendQuestDialogAsync(conn, targetObjId, 2034, ct),
                            12 => await SendQuestDialogAsync(conn, targetObjId, 2120, ct),
                            _  => false,
                        };

                    case DialogAction.SELECT_ACTION_1353:
                        if (var != 1) return false;
                        await PlayQuestMovieAsync(conn, player, 20, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 1353, ct);

                    case DialogAction.SETPRO2:
                        return await DefaultCloseDialogAsync(env, conn, _itemDao, 1, 2,
                            reward: false, sameNpc: false,
                            giveItemId: EvidenceItemId, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);

                    case DialogAction.SETPRO3:
                        return await DefaultCloseDialogAsync(env, conn, _itemDao, 5, 6,
                            reward: false, sameNpc: false,
                            giveItemId: 0, giveItemCount: 0, removeItemId: EvidenceItemId, removeItemCount: 1, ct);

                    case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                        return var switch
                        {
                            6  => await CheckQuestItemsAsync(env, conn, _itemDao, 6, 12, false, 2120, 2205, ct),
                            12 => await SendQuestDialogAsync(conn, targetObjId, 2120, ct),
                            _  => false,
                        };

                    case DialogAction.SETPRO4:
                        return await DefaultCloseDialogAsync(env, conn, 12, 13, ct);

                    case DialogAction.FINISH_DIALOG:
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);

                    default:
                        return false;
                }

            case NpcSleepingElder:
                if (dialog != DialogAction.USE_OBJECT) return false;
                if (player.Inventory.FindByItemId(EvidenceItemId)?.Count != 1) return false;

                return var switch
                {
                    2 => await UseQuestObjectAsync(env, conn, 2, 4, reward: false, dieObject: false, ct),
                    4 => await UseQuestObjectAsync(env, conn, 4, 5, reward: false, dieObject: false, ct),
                    _ => false,
                };

            case NpcDaminu:
                return dialog switch
                {
                    DialogAction.QUEST_SELECT when var == 13 => await SendQuestDialogAsync(conn, targetObjId, 2375, ct),
                    DialogAction.QUEST_SELECT when var == 14 => await SendQuestDialogAsync(conn, targetObjId, 2461, ct),
                    // SETPRO5 collapses Java's instance-teleport detour (var 13->20) directly to
                    // var 14 — see file header for why.
                    DialogAction.SETPRO5 => await DefaultCloseDialogAsync(env, conn, 13, 14, ct),
                    DialogAction.SETPRO6 => await DefaultCloseDialogAsync(env, conn, 14, 14, reward: true, sameNpc: false, ct),
                    _                     => false,
                };

            default:
                return false;
        }
    }
}
