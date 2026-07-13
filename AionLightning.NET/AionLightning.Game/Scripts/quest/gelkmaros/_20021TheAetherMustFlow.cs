// Port of Java data/scripts/system/handlers/quest/gelkmaros/_20021TheAetherMustFlow.java (Gigi, reworked vlog/apozema).
// Asmodian counterpart of inggison._10021FriendsForLife, now portable via EnterInstanceAsync (the
// Elyos version collapsed the whole instance excursion because InstanceService was unported).
// Valetta (799226, var 0->1) -> Angrad (799247, 1->2) -> Eddas (799250, 2->3) -> Taloc's Guardian
// (799325, 3->4, then kill 5 Sticky Sludgers (215992, var1) + 5 Whirling Seafoam (215995, var2),
// last kill flips var0 4->5; back to Guardian SETPRO6 gives Taloc Fruit (182207604) + Taloc's Tears
// (182207603) and enters solo instance 300190000, 5->6). Inside: use the Fruit once (6->7), then use
// the Tears 20 times (var3 0..19, then var0->8) -> kill Celestius (215488, 8->9) -> leaving the
// instance drops the items and rolls var0 9->10 -> Taloc's Mirage (799503, 9->10)... (out-of-instance
// continuation) -> Denskel (799258, 10->11) -> Vellun (799239, SET_SUCCEED flips to REWARD). Turn in
// at Valetta (799226).
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

namespace Quest.Gelkmaros;

public sealed class _20021TheAetherMustFlow : QuestHandlerBase
{
    private const int QuestIdConst    = 20021;
    private const int InstanceWorldId = 300190000;
    private const int FruitItem       = 182207604; // Taloc Fruit
    private const int TearsItem       = 182207603; // Taloc's Tears

    private static readonly int[] _npcIds = { 799226, 799247, 799250, 799325, 799503, 799258, 799239 };

    private readonly IItemDao _itemDao;

    public _20021TheAetherMustFlow(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        RegisterOnDie(engine);
        engine.RegisterQuestNpc(215992).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(215995).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(215488).OnKill.Add(QuestId);
        foreach (int npcId in _npcIds)
            engine.RegisterQuestNpc(npcId).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(FruitItem, QuestId);
        engine.RegisterQuestItem(TearsItem, QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 20000, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);
        int var         = entry.GetVar(0);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == 799226) // Valetta
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status != QuestStatus.START) return false;

        // Java switch fallthrough (QUEST_SELECT -> SETPROx): harmless, DefaultCloseDialog self-guards var.
        if (targetId == 799226) // Valetta
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return false;
        }

        if (targetId == 799247) // Angrad
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO2)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            return false;
        }

        if (targetId == 799250) // Eddas
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 2)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SETPRO3)
                return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
            return false;
        }

        if (targetId == 799325) // Taloc's Guardian
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 3)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (entry.GetVar(1) == 5 || entry.GetVar(2) == 5 || var == 4)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 5, toReward: false, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                }
                return false;
            }
            if (dialog == DialogAction.SETPRO4)
                return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
            if (dialog == DialogAction.SETPRO6)
            {
                if (player.Group is not null)
                    return await SendQuestDialogAsync(conn, targetObjId, 2717, ct);
                if (await GiveQuestItemAsync(player, conn, _itemDao, TearsItem, 1, ct)
                    && await GiveQuestItemAsync(player, conn, _itemDao, FruitItem, 1, ct))
                {
                    await EnterInstanceAsync(player, conn, InstanceWorldId, 202.26694f, 226.0532f, 1098.236f, 30, ct);
                    await ChangeQuestStepAsync(conn, entry, 0, 6, toReward: false, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                // note: Java shows STR_MSG_FULL_INVENTORY here - dropped (cosmetic bag-full notice).
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            if (dialog == DialogAction.FINISH_DIALOG)
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            return false;
        }

        if (targetId == 799503) // Taloc's Mirage
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 9)
                return await SendQuestDialogAsync(conn, targetObjId, 4080, ct);
            if (dialog == DialogAction.SETPRO10)
                return await DefaultCloseDialogAsync(env, conn, 9, 10, ct);
            return false;
        }

        if (targetId == 799258) // Denskel
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 10)
                return await SendQuestDialogAsync(conn, targetObjId, 1267, ct);
            if (dialog == DialogAction.SETPRO11)
                return await DefaultCloseDialogAsync(env, conn, 10, 11, ct);
            return false;
        }

        if (targetId == 799239) // Vellun
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 11)
                return await SendQuestDialogAsync(conn, targetObjId, 1608, ct);
            if (dialog == DialogAction.SET_SUCCEED)
                return await DefaultCloseDialogAsync(env, conn, 11, 11, reward: true, sameNpc: false, ct);
            return false;
        }

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int targetId = env.TargetId;
        int var0 = entry.GetVar(0), var1 = entry.GetVar(1), var2 = entry.GetVar(2);

        switch (targetId)
        {
            case 215992: // Sticky Sludger
                if (var1 < 5 && var0 == 4)
                {
                    entry.SetVar(1, var1 + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return true;
                }
                if (var1 == 4 && var2 == 5 && var0 == 4)
                {
                    entry.SetVar(0, var0 + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return true;
                }
                break;
            case 215995: // Whirling Seafoam
                if (var2 < 5 && var0 == 4)
                {
                    entry.SetVar(2, var2 + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return true;
                }
                if (var1 == 5 && var2 == 4 && var0 == 4)
                {
                    entry.SetVar(0, var0 + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return true;
                }
                break;
            case 215488: // Celestius
                if (var0 == 8)
                    return await DefaultOnKillEventAsync(env, conn, 215488, 8, 9, ct);
                break;
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (player.Position.WorldId != InstanceWorldId) return false;

        int var0 = entry.GetVar(0);
        int var3 = entry.GetVar(3);

        if (itemId == FruitItem) // Taloc Fruit
        {
            // note: Java TODO says this should not consume the item (skill still fires); kept as a plain step transition (6->7).
            await ChangeQuestStepAsync(conn, entry, 0, 7, toReward: false, ct);
            return true;
        }
        if (itemId == TearsItem) // Taloc's Tears
        {
            if (var0 == 7)
            {
                if (var3 >= 0 && var3 < 19)
                {
                    await ChangeQuestStepAsync(conn, entry, 3, var3 + 1, toReward: false, ct);
                    return true;
                }
                if (var3 == 19)
                {
                    entry.Step = 8; // Java: qs.setQuestVar(8) (packed overwrite - clears var3)
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return true;
                }
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (player.Position.WorldId == InstanceWorldId) return false;

        int var0 = entry.GetVar(0);
        if (var0 >= 6 && var0 < 9)
        {
            await RemoveQuestItemAsync(player, conn, _itemDao, FruitItem, 1, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, TearsItem, 1, ct);
            entry.Step = 5; // Java: qs.setQuestVar(5)
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        if (var0 == 9)
        {
            await RemoveQuestItemAsync(player, conn, _itemDao, FruitItem, 1, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, TearsItem, 1, ct);
            entry.Step = 10; // Java: qs.setQuestVar(10)
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var0 = entry.GetVar(0);
        if (var0 >= 6 && var0 < 9)
        {
            await RemoveQuestItemAsync(player, conn, _itemDao, FruitItem, 1, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, TearsItem, 1, ct);
            entry.Step = 5; // Java: qs.setQuestVar(5)
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }
}
