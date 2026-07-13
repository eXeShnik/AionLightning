// Port of Java data/scripts/system/handlers/quest/fort_tiamat/_10070KaisinelsCommand.java (Cheatkiller).
// Elyos instance chain (Fort Tiamat / world 300490000): 205579 start -> 798600 (gives token 182213241)
// -> 205579 (removes token) -> 205842 -> object 730628 (enters instance 300490000) -> object 730691
// (spawns Kaisinel 800386) -> 800386 (movie 493, spawns 800385/800431/800352) -> 800431 -> 800352
// (SET_SUCCEED). Turn in at 205842.
// Unblocked by EnterInstanceAsync (Java InstanceService.getNextAvailableInstance triad).
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

namespace Quest.FortTiamat;

public sealed class _10070KaisinelsCommand : QuestHandlerBase
{
    private const int QuestIdConst = 10070;
    private const int InstanceWorldId = 300490000;
    private const int TokenItem = 182213241;

    private readonly IItemDao _itemDao;

    public _10070KaisinelsCommand(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        int[] npcs = { 205579, 798600, 205842, 730628, 730625, 800386, 800431, 800352, 730691 };
        foreach (int npc in npcs)
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 10064, ct);

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (player.Position.WorldId != InstanceWorldId && entry.GetVar(0) >= 5)
        {
            entry.SetVar(0, 3);
            await UpdateQuestStatusAsync(conn, entry, ct);
            // note: Java also sends SM_SYSTEM_MESSAGE QUEST_FAILED_$1 here - cosmetic notice, dropped.
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != 205842) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);

        if (targetId == 205579)
        {
            // Java switch fallthrough (QUEST_SELECT -> SETPROx): harmless, DefaultCloseDialog self-guards var.
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 0:
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                case DialogAction.QUEST_SELECT when var == 2:
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                case DialogAction.SETPRO1:
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                case DialogAction.SETPRO3 when var == 2:
                    await RemoveQuestItemAsync(player, conn, _itemDao, TokenItem, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                default:
                    return false;
            }
        }

        if (targetId == 798600)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 1:
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                case DialogAction.SETPRO2 when var == 1:
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, TokenItem, 1, ct)) return false;
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                default:
                    return false;
            }
        }

        if (targetId == 205842)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 3:
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                case DialogAction.SETPRO4:
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                default:
                    return false;
            }
        }

        if (targetId == 730628)
        {
            switch (dialog)
            {
                case DialogAction.USE_OBJECT when var == 4:
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                case DialogAction.SETPRO5 when var == 4:
                    await EnterInstanceAsync(player, conn, InstanceWorldId, 497f, 507f, 241f, 0, ct);
                    await ChangeQuestStepAsync(conn, entry, 0, 5, toReward: false, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                default:
                    return false;
            }
        }

        if (targetId == 730691)
        {
            switch (dialog)
            {
                case DialogAction.USE_OBJECT when var == 5:
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                case DialogAction.SETPRO6 when var == 5:
                    // note: Java intra-instance TeleportService2 relocation (549,525,417) dropped - no equivalent.
                    SpawnQuestNpc(InstanceWorldId, player.Position.InstanceId, 800386, 458f, 514f, 417f, 119);
                    await ChangeQuestStepAsync(conn, entry, 0, 6, toReward: false, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                default:
                    return false;
            }
        }

        if (targetId == 800386)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 6:
                    return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                case DialogAction.SETPRO7 when var == 6:
                    await PlayQuestMovieAsync(conn, player, 493, ct);
                    // note: Java despawns the talked-to NPC here - no despawn API, dropped (cosmetic).
                    SpawnQuestNpc(InstanceWorldId, player.Position.InstanceId, 800385, 458f, 514f, 417f, 119);
                    SpawnQuestNpc(InstanceWorldId, player.Position.InstanceId, 800431, 502f, 526f, 417f, 70);
                    SpawnQuestNpc(InstanceWorldId, player.Position.InstanceId, 800352, 499f, 500f, 417f, 53);
                    return await DefaultCloseDialogAsync(env, conn, 6, 7, ct);
                default:
                    return false;
            }
        }

        if (targetId == 800431)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 7:
                    return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                case DialogAction.SETPRO8 when var == 7:
                    // note: Java despawns the talked-to NPC here - no despawn API, dropped (cosmetic).
                    return await DefaultCloseDialogAsync(env, conn, 7, 8, ct);
                default:
                    return false;
            }
        }

        if (targetId == 800352)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 8:
                    return await SendQuestDialogAsync(conn, targetObjId, 3739, ct);
                case DialogAction.SET_SUCCEED when var == 8:
                    // note: Java despawns the talked-to NPC here - no despawn API, dropped (cosmetic).
                    return await DefaultCloseDialogAsync(env, conn, 8, 9, reward: true, sameNpc: false, ct);
                default:
                    return false;
            }
        }

        return false;
    }
}
