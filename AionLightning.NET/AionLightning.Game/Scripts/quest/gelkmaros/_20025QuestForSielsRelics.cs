// Port of Java data/scripts/system/handlers/quest/gelkmaros/_20025QuestForSielsRelics.java (Nephis,
// reworked Gigi). Richelle (799225, var0->1) -> Valetta (799226: var1->2, var4->5, var9->10) ->
// a guide npc (799341: var2->3, var3->4) -> 798800 (var5->6) -> 204182 (var6->7) -> Vellun
// (799239, var7->8) -> 204837 (var8->9, collect-item check via CheckQuestItemsAsync) -> 799327
// (var10->11) -> 799328 (var11->12) -> entering Beshmundir's Walk at var12 bumps to 13 -> killing
// 799342 at var13 flips straight to REWARD; turn in at Richelle. NPC 799342's OnKill registration
// mirrors the same id used (inertly) by _20020 — here it's the actual completion trigger.
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

namespace Quest.Gelkmaros;

public sealed class _20025QuestForSielsRelics : QuestHandlerBase
{
    private const int QuestIdConst  = 20025;
    private const int RichelleNpc   = 799225;
    private const int ValettaNpc    = 799226;
    private const int GuideNpc      = 799341;
    private const int Npc798800     = 798800;
    private const int Npc204182     = 204182;
    private const int VellunNpc     = 799239;
    private const int CollectNpc    = 204837;
    private const int Npc799327     = 799327;
    private const int Npc799328     = 799328;
    private const int RelicKillerNpc = 799342;
    private const string RelicsZone = "BESHMUNDIRS_WALK_300170000";

    private static readonly int[] _talkNpcs =
        [RichelleNpc, ValettaNpc, GuideNpc, Npc798800, Npc204182, VellunNpc, CollectNpc, Npc799327, Npc799328];

    private readonly IItemDao _itemDao;

    public _20025QuestForSielsRelics(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(RelicKillerNpc).OnKill.Add(QuestId);
        RegisterOnEnterZone(engine, RelicsZone);
        engine.RegisterOnZoneMissionEnd(QuestId);
        foreach (int npc in _talkNpcs) engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != RelicsZone) return false;

        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.GetVar(0) != 12) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 13, toReward: false, ct);
        return true;
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.TargetId != RelicKillerNpc) return false;
        if (entry.GetVar(0) != 13) return false;

        await ChangeQuestStepAsync(conn, entry, -1, 0, toReward: true, ct);
        return true;
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 20024, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == RichelleNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }
        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);

        if (targetId == RichelleNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return false;
        }

        if (targetId == ValettaNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (var == 9) return await SendQuestDialogAsync(conn, targetObjId, 4080, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO2)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            if (dialog == DialogAction.SETPRO5)
                return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
            if (dialog == DialogAction.SETPRO10)
                return await DefaultCloseDialogAsync(env, conn, 9, 10, ct);
            return false;
        }

        if (targetId == GuideNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO3)
                return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
            if (dialog == DialogAction.SETPRO4)
                return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
            return false;
        }

        if (targetId == Npc798800)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 5)
                return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
            if (dialog == DialogAction.SETPRO6)
                return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
            return false;
        }

        if (targetId == Npc204182)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 6)
                return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
            if (dialog == DialogAction.SETPRO7)
                return await DefaultCloseDialogAsync(env, conn, 6, 7, ct);
            return false;
        }

        if (targetId == VellunNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 7)
                return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
            if (dialog == DialogAction.SETPRO8)
                return await DefaultCloseDialogAsync(env, conn, 7, 8, ct);
            return false;
        }

        if (targetId == CollectNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 8)
                return await SendQuestDialogAsync(conn, targetObjId, 3739, ct);
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 8, 9, false, 10, 10001, ct);
            return false;
        }

        if (targetId == Npc799327)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 10)
                return await SendQuestDialogAsync(conn, targetObjId, 1267, ct);
            if (dialog == DialogAction.SETPRO11)
                return await DefaultCloseDialogAsync(env, conn, 10, 11, ct);
            return false;
        }

        if (targetId == Npc799328)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 11)
                return await SendQuestDialogAsync(conn, targetObjId, 1608, ct);
            if (dialog == DialogAction.SETPRO12)
                return await DefaultCloseDialogAsync(env, conn, 11, 12, ct);
            return false;
        }

        return false;
    }
}
