// Port of Java data/scripts/system/handlers/quest/inggison/_10025ItsBetterItsAnObelisk.java (Nephis).
// Chain quest following _10024WillTheAetherRain: five npcs (798927, 798932, 798933, 799023, 799024,
// 278500) drive var 0 through 9 via talk dialogs; 799024 gives loop items 182206623 (var 3->4) and
// 182206624 (var 5->6). Using 182206623 while inside the Temple of Scales zone advances var 4->5;
// using 182206624 inside the Altar of Avarice zone advances var 6->7; using 182206626 inside the
// Angrief Bulwark zone flips straight to REWARD. Two plain "use object" gadgets (700607, 700637)
// bump var 10->11->12 without any reward/status broadcast beyond the raw quest-var update (matches
// Java exactly: onDialogEvent falls through to its final `return false` for both, i.e. the mutation
// happens but the dialog is reported unhandled). Entering BESHMUNDIRS_WALK_300170000 at var 9 bumps
// to var 10 (the zone-enter hook this batch adds).
// Skip vs Java: onItemUseEvent's 3s SM_ITEM_USAGE_ANIMATION cast delay before each item-use effect
// is cosmetic-only and dropped, same simplification as inggison._11031CanIEatIt /
// beluslan._2670TheAncientBook - the item removal + step advance happen immediately instead.
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

namespace Quest.Inggison;

public sealed class _10025ItsBetterItsAnObelisk : QuestHandlerBase
{
    private const int QuestIdConst = 10025;
    private const int FirstNpc      = 798927;
    private const int SecondNpc     = 798932;
    private const int ThirdNpc      = 798933;
    private const int FourthNpc     = 799023;
    private const int FifthNpc      = 799024;
    private const int SixthNpc      = 278500;
    private const int TurnInNpc     = 798926;
    private const int FirstGadget   = 700607;
    private const int SecondGadget  = 700637;
    private const int FirstStoneItem  = 182206623;
    private const int SecondStoneItem = 182206624;
    private const int ThirdStoneItem  = 182206626;
    private const int PrecedingQuest  = 10024;
    private const string EnterZoneName        = "BESHMUNDIRS_WALK_300170000";
    private const string TempleOfScalesZone   = "TEMPLE_OF_SCALES_210050000";
    private const string AltarOfAvariceZone   = "ALTAR_OF_AVARICE_210050000";
    private const string AngriefBulwarkZone   = "ANGRIEF_BULWARK_210050000";

    private static readonly int[] TalkNpcs =
        { FirstNpc, SecondNpc, ThirdNpc, FourthNpc, FifthNpc, SixthNpc, TurnInNpc, FirstGadget, SecondGadget };

    private readonly IItemDao _itemDao;

    public _10025ItsBetterItsAnObelisk(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestItem(FirstStoneItem, QuestId);
        engine.RegisterQuestItem(SecondStoneItem, QuestId);
        engine.RegisterQuestItem(ThirdStoneItem, QuestId);
        RegisterOnEnterZone(engine, EnterZoneName);
        foreach (int npcId in TalkNpcs)
            engine.RegisterQuestNpc(npcId).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, PrecedingQuest, isZoneMission: true, ct);

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != EnterZoneName) return false;

        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.GetVar(0) != 9) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 10, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        if (entry is null) return false;
        int var = entry.GetVar(0);

        if (var == 4 && itemId == FirstStoneItem)
        {
            if (!player.CurrentZones.Contains(TempleOfScalesZone)) return false;
            await RemoveQuestItemAsync(player, conn, _itemDao, FirstStoneItem, 1, ct);
            await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
            return true;
        }
        if (var == 6 && itemId == SecondStoneItem)
        {
            if (!player.CurrentZones.Contains(AltarOfAvariceZone)) return false;
            await RemoveQuestItemAsync(player, conn, _itemDao, SecondStoneItem, 1, ct);
            await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
            return true;
        }
        if (var == 12 && itemId == ThirdStoneItem)
        {
            if (!player.CurrentZones.Contains(AngriefBulwarkZone)) return false;
            await RemoveQuestItemAsync(player, conn, _itemDao, ThirdStoneItem, 1, ct);
            entry.Status = QuestStatus.REWARD;
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);
        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != TurnInNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        if (entry.Status != QuestStatus.START) return false;

        if (targetId == FirstNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1 && var == 0) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return false;
        }
        if (targetId == SecondNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO2 && var == 1) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            return false;
        }
        if (targetId == ThirdNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SETPRO3 && var == 2) return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
            return false;
        }
        if (targetId == FourthNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            if (dialog == DialogAction.SETPRO4 && var == 3)
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 3, 4, false, false, FirstStoneItem, 1, 0, 0, ct);
            return false;
        }
        if (targetId == FifthNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 5) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (var == 7) return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
            }
            if (dialog == DialogAction.SETPRO6 && var == 5)
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 5, 6, false, false, SecondStoneItem, 1, 0, 0, ct);
            if (dialog == DialogAction.SETPRO8 && var == 7)
                return await DefaultCloseDialogAsync(env, conn, 7, 8, ct);
            return false;
        }
        if (targetId == SixthNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 8) return await SendQuestDialogAsync(conn, targetObjId, 3739, ct);
            if (dialog == DialogAction.SETPRO9 && var == 8) return await DefaultCloseDialogAsync(env, conn, 8, 9, ct);
            return false;
        }
        if (targetId == FirstGadget)
        {
            // Java bug: mutates the var but falls through to the method's final `return false`
            // (no dialog is shown) - preserved as-is.
            if (dialog == DialogAction.USE_OBJECT && var == 10)
                await ChangeQuestStepAsync(conn, entry, 0, 11, toReward: false, ct);
            return false;
        }
        if (targetId == SecondGadget)
        {
            if (dialog == DialogAction.USE_OBJECT && var == 11)
                await ChangeQuestStepAsync(conn, entry, 0, 12, toReward: false, ct);
            return false;
        }
        return false;
    }
}
