// Port of Java data/scripts/system/handlers/quest/beluslan/_2054LightuptheLighthouse.java.
// 204768 (var0->1), 204739 (var1->2, plays movie 237 on SELECT_ACTION_1353), using the quest
// item 182204308 at var2 advances to var3, object 730109 spawns npc 213912 at var3 (no step
// change), object 730140 gives 182204309 and advances var3->4, use-object 700287 at var4 removes
// 182204309, plays movie 238, and flips to REWARD. Skip vs Java: the SM_ITEM_USAGE_ANIMATION cast
// delay on item use, and the "despawn+respawn" visual reset of 730109/730140 after interaction
// (Java npc.getController().onDelete()/scheduleRespawn(), no NPC despawn/respawn control ported)
// are omitted — cosmetic only, state transitions are unaffected.
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

namespace Quest.Beluslan;

public sealed class _2054LightuptheLighthouse : QuestHandlerBase
{
    private const int QuestIdConst = 2054;
    private const int Npc768 = 204768;
    private const int Npc739 = 204739;
    private const int Obj109 = 730109;
    private const int Obj140 = 730140;
    private const int Obj287 = 700287;
    private const int LampItem = 182204308;
    private const int OilItem = 182204309;
    private const int BeluslanWorldId = 220040000;
    private const int SpawnMobNpc = 213912;

    private readonly IItemDao _itemDao;

    public _2054LightuptheLighthouse(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(LampItem, QuestId);
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(Npc768).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc739).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Obj109).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Obj140).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Obj287).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 2500, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var = entry.GetVar(0);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == Npc768)
            {
                if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD) return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }
        if (entry.Status != QuestStatus.START) return false;

        if (targetId == Npc768)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return false;
        }
        if (targetId == Npc739)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SELECT_ACTION_1353)
            {
                await PlayQuestMovieAsync(conn, env.Player, 237, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            return false;
        }
        if (targetId == Obj109)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            if (dialog == DialogAction.SETPRO4 && var == 3)
            {
                var pos = env.Target!.Position;
                SpawnQuestNpc(BeluslanWorldId, 1, SpawnMobNpc, pos.X, pos.Y, pos.Z, 0);
                await CloseDialogWindowAsync(conn, targetObjId, ct);
                return true;
            }
            return false;
        }
        if (targetId == Obj140)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2120, ct);
            if (dialog == DialogAction.SETPRO5 && var == 3)
            {
                if (!await GiveQuestItemAsync(env.Player, conn, _itemDao, OilItem, 1, ct)) return true;
                await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: false, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            return false;
        }
        if (targetId == Obj287 && var == 4)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await UseQuestObjectAsync(env, conn, 4, 4, true, 0, 0, 0, OilItem, 1, 238, false, _itemDao, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != LampItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.GetVar(0) != 2) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, LampItem, 1, ct);
        entry.SetVar(0, 3);
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }
}
