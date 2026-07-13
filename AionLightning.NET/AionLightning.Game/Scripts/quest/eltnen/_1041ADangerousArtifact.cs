// Port of Java data/scripts/system/handlers/quest/eltnen/_1041ADangerousArtifact.java (Xitanium, reworked vlog).
// Talk Telemachus (203901, var0 0->1); escort the Civil Engineer (204015) to a sensory area
// (var0 1->2, on reach var0 2->3, lost/logout rolls 2->1); back to Telemachus (SETPRO3 var0 3->4);
// talk Xenophon (203833, var0 4->5); talk Yuditio (278500, var0 5->6); back to Telemachus (var0 6->7);
// talk Laigas (204042) to receive the stolen artifact item (182201011); use the Stolen Artifact object
// (700181, var0 8->9); back to Laigas (var0 9 -> REWARD, movie 38); turn in at Telemachus.
// Uses the follow/escort subsystem (StartFollowToZone + reach/lost hooks).
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

namespace Quest.Eltnen;

public sealed class _1041ADangerousArtifact : QuestHandlerBase
{
    private const int QuestIdConst  = 1041;
    private const int TelemachusNpc = 203901;
    private const int EngineerNpc   = 204015;
    private const int XenophonNpc   = 203833;
    private const int YuditioNpc    = 278500;
    private const int LaigasNpc     = 204042;
    private const int ArtifactObj   = 700181;
    private const int ArtifactItem  = 182201011;
    private const string ZoneA = "LF2_SENSORYAREA_Q1041_A_206040_21_210020000";

    private static readonly int[] _npcs = [TelemachusNpc, EngineerNpc, XenophonNpc, YuditioNpc, LaigasNpc, ArtifactObj];

    private readonly IItemDao _itemDao;

    public _1041ADangerousArtifact(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterItemGet(ArtifactItem, QuestId);
        RegisterOnLogOut(engine);
        RegisterOnReachTarget(engine);
        RegisterOnLostTarget(engine);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npc in _npcs)
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 1300, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);
        int var         = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == TelemachusNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (var == 6) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                }
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                if (dialog == DialogAction.SETPRO3)
                {
                    // note: TeleportService2 relocation (110010000 2044/1486/581) dropped — plain relocation, state kept
                    await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: false, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                if (dialog == DialogAction.SETPRO6) return await DefaultCloseDialogAsync(env, conn, 6, 7, ct);
                return false;
            }

            if (targetId == EngineerNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    StartFollowToZone(env, conn, (Npc)env.Target!, ZoneA);
                    // note: Java second reach-zone (LF2_SENSORYAREA_Q1041_B_206042_23_210020000) dropped — single-zone follow helper; zone A gates the escort
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                }
                return false;
            }

            if (targetId == XenophonNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO4) return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                return false;
            }

            if (targetId == YuditioNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 5) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO5)
                {
                    // note: TeleportService2 relocation (210020000 267/2790/272) dropped — plain relocation, state kept
                    await ChangeQuestStepAsync(conn, entry, 0, 6, toReward: false, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return false;
            }

            if (targetId == LaigasNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 7) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                    if (var == 9) return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                }
                if (dialog == DialogAction.SETPRO7)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, ArtifactItem, 1, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                if (dialog == DialogAction.SETPRO8)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 9, toReward: true, ct);
                    await PlayQuestMovieAsync(conn, player, 38, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return false;
            }

            if (targetId == ArtifactObj && dialog == DialogAction.USE_OBJECT)
                return await UseQuestObjectAsync(env, conn, 8, 9, reward: false, 0, 0, 0, ArtifactItem, 1, 0, false, _itemDao, ct);

            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TelemachusNpc)
        {
            if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 3739, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnItemGetAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) == 7)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 8, toReward: false, ct);
            await PlayQuestMovieAsync(conn, player, 37, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnLogOutAsync(QuestEnv env, GsClientConnection? conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) == 2)
        {
            entry.SetVar(0, 1);
            if (conn is not null) await UpdateQuestStatusAsync(conn, entry, ct);
            else await QuestDao.UpsertAsync(player.ObjectId, entry, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnNpcReachTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 2) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnNpcLostTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 2) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
        return true;
    }
}
