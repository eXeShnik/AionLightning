// Port of Java data/scripts/system/handlers/quest/morheim/_2037TheProtectorofNepra.java (Hellboy aion4Free).
// Zone-mission chain quest (auto-started via OnLevelUpAsync/OnZoneMissionEndAsync once 2300 is
// COMPLETE). Talk 204369 (var 0->1, movie 80 on SELECT_ACTION_1012), then 204361 through var 1->2,
// 278004 hands out item 182204015 at var 2->3, back to 204361 (var 3->4, consumes the item),
// entering ALTAR_OF_THE_BLACK_DRAGON_220020000 at var 4 advances to var 5 (movie 81), 204361 again
// var 5->6, killing 212861 bumps var 6->7, 204361's SET_SUCCEED flips to REWARD. Turn in at 204369.
// Java quirk not replicated: targetId 204369's case in the original switch has no trailing break, so
// it structurally falls into the 204361 case body when the inner dialog switch doesn't return - this
// is only reachable if the client sent a dialog id belonging to 204361's conversation while the
// actual target was 204369, which cannot happen through normal client interaction (dialog ids are
// scoped to the npc actually being talked to); each targetId branch is implemented independently.
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

namespace Quest.Morheim;

public sealed class _2037TheProtectorofNepra : QuestHandlerBase
{
    private const int QuestIdConst = 2037;
    private const int NinaNpc      = 204369;
    private const int KargateNpc   = 204361;
    private const int GuardianNpc  = 278004;
    private const int WolfNpc      = 212861;
    private const int TokenItem    = 182204015;
    private const string DragonAltarZone = "ALTAR_OF_THE_BLACK_DRAGON_220020000";

    private readonly IItemDao _itemDao;

    public _2037TheProtectorofNepra(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(WolfNpc).OnKill.Add(QuestId);
        RegisterOnEnterZone(engine, DragonAltarZone);
        foreach (int npc in new[] { NinaNpc, KargateNpc, GuardianNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 2300, isZoneMission: true, ct);

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => await DefaultOnKillEventAsync(env, conn, WolfNpc, 6, 7, ct);

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != DragonAltarZone) return false;
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.GetVar(0) != 4) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 5, toReward: false, ct);
        await PlayQuestMovieAsync(conn, env.Player, 81, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);
        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == NinaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 0 && await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SELECT_ACTION_1012)
                {
                    await PlayQuestMovieAsync(conn, player, 80, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO1)
                    return var == 0 && await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }

            if (targetId == KargateNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 3 && (player.Inventory.FindByItemId(TokenItem)?.Count ?? 0) == 1)
                        return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    if (var == 5) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                    if (var == 7) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO2)
                    return var == 1 && await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                if (dialog == DialogAction.SETPRO4)
                {
                    if (var != 3) return false;
                    await RemoveQuestItemAsync(player, conn, _itemDao, TokenItem, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                }
                if (dialog == DialogAction.SETPRO6)
                    return var == 5 && await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                    return var == 7 && await DefaultCloseDialogAsync(env, conn, 7, 7, reward: true, sameNpc: false, ct);
                return false;
            }

            if (targetId == GuardianNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 2 && await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3)
                {
                    if (var != 2) return false;
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, TokenItem, 1, ct)) return true;
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                }
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == NinaNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
