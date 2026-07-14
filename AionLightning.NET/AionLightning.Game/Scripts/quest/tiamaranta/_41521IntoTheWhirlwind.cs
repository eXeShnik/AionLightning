// Port of Java data/scripts/system/handlers/quest/tiamaranta/_41521IntoTheWhirlwind.java (Cheatkiller).
// Accept at 205962 (grants item 182212525). Using that item inside zone LDF4B_ITEMUSEAREA_Q41521A while
// at var0 0/3 applies abnormal effect 2252 (60s); talking to 701134 while that effect is active advances
// var0 0->1 and teleports the player to the arena. Talk 205963 (var0 1->2), then kill 6x {218278/218279}
// and 8x {218284/218285} (var0 2->3), then hand in at 205963 (removes the item, flips to REWARD).
// Turn in at 205962.
// note: the abnormal-effect leg is the core gate and cannot be expressed — there is no quest-callable
// effect apply/check API in this port (OnItemUse carries no zone info either). Both the SkillEngine
// applyEffectDirectly(2252) in the item-use leg and the hasAbnormalEffect(2252) gate on the 701134 talk
// are therefore dropped: talking to 701134 while the quest is at START simply advances var0 0->1, so the
// quest stays completable. The subsequent TeleportService2 relocation (which does not gate progression,
// var0 is already advanced) is dropped too — no script-callable teleport helper exists.
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

namespace Quest.Tiamaranta;

public sealed class _41521IntoTheWhirlwind : QuestHandlerBase
{
    private const int QuestIdConst = 41521;
    private const int StartNpc = 205962;
    private const int EffectNpc = 701134;
    private const int TurnInNpc = 205963;
    private const int QuestItem = 182212525;

    private static readonly int[] Mobs = { 218278, 218279, 218284, 218285 };

    private readonly IItemDao _itemDao;

    public _41521IntoTheWhirlwind(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(QuestItem, QuestId);
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EffectNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        foreach (int mob in Mobs)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
                    await GiveQuestItemAsync(player, conn, _itemDao, QuestItem, 1, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }
        else if (entry.Status == QuestStatus.START)
        {
            // note: Java gates this on player.hasAbnormalEffect(2252); the effect can't be applied/checked
            // in this port, so the gate is dropped — the talk advances var0 0->1 unconditionally.
            if (targetId == EffectNpc)
            {
                if (entry.GetVar(0) == 0)
                    await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                // note: Java teleports to (1097.74, 124.24, 61.56) here — no script-callable teleport; dropped.
                return true;
            }
            else if (targetId == TurnInNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, entry.GetVar(0) == 3 ? 2034 : 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    if (entry.GetVar(0) == 3)
                    {
                        await RemoveQuestItemAsync(player, conn, _itemDao, QuestItem, 1, ct);
                        return await DefaultCloseDialogAsync(env, conn, 3, 3, reward: true, sameNpc: false, ct);
                    }
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) != 2) return false;
        await CheckAndUpdateVarMobsAsync(entry, conn, env.TargetId, ct);
        return false;
    }

    private async ValueTask CheckAndUpdateVarMobsAsync(QuestEntry entry, GsClientConnection conn, int targetId, CancellationToken ct)
    {
        int var1 = entry.GetVar(1);
        int var2 = entry.GetVar(2);
        // Java switch fallthrough: 218278/218279 share the var1 counter, 218284/218285 the var2 counter.
        if (targetId == 218278 || targetId == 218279)
        {
            if (var1 != 6)
            {
                entry.SetVar(1, var1 + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
            }
            await IsAllKilledMobsAsync(entry, conn, ct);
        }
        else if (targetId == 218284 || targetId == 218285)
        {
            if (var2 != 8)
            {
                entry.SetVar(2, var2 + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
            }
            await IsAllKilledMobsAsync(entry, conn, ct);
        }
    }

    private async ValueTask IsAllKilledMobsAsync(QuestEntry entry, GsClientConnection conn, CancellationToken ct)
    {
        if (entry.GetVar(1) == 6 && entry.GetVar(2) == 8)
        {
            entry.SetVar(1, 0);
            entry.SetVar(2, 0);
            await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
        }
    }

    public override ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != QuestItem) return ValueTask.FromResult(false);
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return ValueTask.FromResult(false);
        // note: Java applies abnormal effect 2252 (SkillEngine.applyEffectDirectly) when var0 is 0/3 and the
        // player stands in zone LDF4B_ITEMUSEAREA_Q41521A. Neither the effect-apply API nor the item-use
        // zone context exist in this port, so the effect is dropped (see class summary). Item use is a no-op.
        return ValueTask.FromResult(true);
    }
}
