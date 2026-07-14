// Port of Java data/scripts/system/handlers/quest/theobomos/_3088InCiderTrading.java (Cheatkiller).
// A "spend Apple Cider (160003020)" trade quest with three mutually-exclusive branches chosen at the
// start NPC 798202: spend 1 (var0->2, turn in at 798201), spend 10 (var0->4->5->6, turn in at 798132)
// or spend 100 (var0->8, turn in at 798166 after collecting item 182208064). Each branch pays a
// different <rewards> block (index 0/1/2 in quest_data.xml), selected here via FinishQuestAsync so the
// per-path reward is fully modelled — no reward-selection drop needed (unlike class-keyed daevation
// ports). Mirrors the SendQuestEndDialogWithReward pattern established by morheim/_2430SecretInformation.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Theobomos;

public sealed class _3088InCiderTrading : QuestHandlerBase
{
    private const int QuestIdConst = 3088;
    private const int StartNpc  = 798202;
    private const int Npc798201 = 798201;
    private const int Npc798204 = 798204;
    private const int Npc798132 = 798132;
    private const int Npc798166 = 798166;
    private const int CiderItem = 160003020;
    private const int PackItem  = 182208064;

    private readonly IItemDao _itemDao;

    public _3088InCiderTrading(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc798201).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc798204).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc798132).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc798166).OnTalk.Add(QuestId);
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
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            long ciderCount = player.Inventory.FindByItemId(CiderItem)?.Count ?? 0;

            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                }
                else if (dialog == DialogAction.SELECT_ACTION_1012)
                    return await SendQuestDialogAsync(conn, targetObjId, ciderCount >= 1 ? 1012 : 1267, ct);
                else if (dialog == DialogAction.SELECT_ACTION_1097)
                    return await SendQuestDialogAsync(conn, targetObjId, ciderCount >= 10 ? 1097 : 1267, ct);
                else if (dialog == DialogAction.SELECT_ACTION_1182)
                    return await SendQuestDialogAsync(conn, targetObjId, ciderCount >= 100 ? 1182 : 1267, ct);
                else if (dialog == DialogAction.SELECT_ACTION_1011)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                else if (dialog == DialogAction.SETPRO1)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, CiderItem, 1, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                }
                else if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 0, 2, ct);
                else if (dialog == DialogAction.SETPRO3)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, CiderItem, 10, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                }
                else if (dialog == DialogAction.SETPRO4)
                    return await DefaultCloseDialogAsync(env, conn, 0, 4, ct);
                else if (dialog == DialogAction.SETPRO7)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, CiderItem, 100, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                }
                else if (dialog == DialogAction.SETPRO8)
                    return await DefaultCloseDialogAsync(env, conn, 0, 8, ct);
            }

            if (targetId == Npc798201)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (var == 5) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                }
                else if (dialog == DialogAction.SETPRO6)
                    return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
                else if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    // Java: defaultCloseDialog(env, 2, 2, true, true) — flip to REWARD, sameNpc sendQuestEndDialog(env,0) shows page 5.
                    if (entry.GetVar(0) != 2) return false;
                    await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
            }

            if (targetId == Npc798204)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                }
                else if (dialog == DialogAction.SETPRO5)
                    return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
            }

            if (targetId == Npc798132)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 6) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                }
                else if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    // Java: defaultCloseDialog(env, 6, 6, true, true) — flip to REWARD, sameNpc shows page 5 (rewardId 0).
                    if (entry.GetVar(0) != 6) return false;
                    await ChangeQuestStepAsync(conn, entry, 0, 6, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
            }

            if (targetId == Npc798166)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 8 && (player.Inventory.FindByItemId(PackItem)?.Count ?? 0) >= 1)
                        return await SendQuestDialogAsync(conn, targetObjId, 3739, ct);
                }
                else if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    // Java: decreaseByItemId(182208064, 1) then defaultCloseDialog(env, 8, 8, true, true) — flip, page 5.
                    if (entry.GetVar(0) != 8) return false;
                    await RemoveQuestItemAsync(player, conn, _itemDao, PackItem, 1, ct);
                    await ChangeQuestStepAsync(conn, entry, 0, 8, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            int var = entry.GetVar(0);
            if (targetId == Npc798201 && var == 2)
            {
                if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogWithRewardAsync(env, conn, 0, ct);
            }
            if (targetId == Npc798132 && var == 6)
            {
                if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogWithRewardAsync(env, conn, 1, ct);
            }
            if (targetId == Npc798166 && var == 8)
            {
                if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogWithRewardAsync(env, conn, 2, ct);
            }
        }

        return false;
    }

    /// <summary>Java QuestHandler.sendQuestEndDialog(env, reward): SELECT_QUEST_REWARD/USE_OBJECT shows the
    /// tier-confirm page 5+reward; SELECTED_QUEST_REWARDn/NOREWARD actually completes with that reward tier.</summary>
    private async ValueTask<bool> SendQuestEndDialogWithRewardAsync(QuestEnv env, GsClientConnection conn, int rewardIndex, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.REWARD) return false;
        int targetObjId = env.Target?.ObjectId ?? 0;
        int dialogId = env.DialogId;

        if (dialogId >= (int)DialogAction.SELECTED_QUEST_REWARD1 && dialogId <= (int)DialogAction.SELECTED_QUEST_NOREWARD)
        {
            if (!await FinishQuestAsync(conn, env.Player, rewardIndex, ct)) return false;
            await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
            return true;
        }
        if (dialogId == (int)DialogAction.SELECT_QUEST_REWARD || dialogId == (int)DialogAction.USE_OBJECT)
            return await SendQuestDialogAsync(conn, targetObjId, 5 + rewardIndex, ct);
        return false;
    }
}
