// Port of Java data/scripts/system/handlers/quest/inggison/_10023SullasStartlingDiscovery.java.
// Yulia (798928, var0->1) -> Sulla (798975, var1->2, later var6->7, var9->10) -> Philon (798981,
// var2->3) -> use the three Petrified Masses (730226/227/228, var3->4->5->6) -> Machiah (798513,
// var7->8) -> Pyrrha (798225, var8->9) -> Gelon (798979, var10->11) -> Titus (798990, var11->12,
// plays movie 504) -> the Drakan Stone Statue (730295, var==12) checks for the Bloody Greathammer
// and finishes the quest.
//
// Skip vs Java (documented deviation): past the statue, Java teleports into a solo instance of
// world 300160000 (InstanceService + TeleportService2, neither ported) where killing Zhanim the
// Librarian (216531) spawns a Traveller's Bag (730229) via QuestService.addNewSpawn, picking up its
// dropped item (182206614, registerGetingItem/addHandlerSideQuestDrop) advances var15->16, and using
// that item finishes the quest (var16, reward=true); onEnterWorldEvent manages re-granting/rolling
// back the Greathammer around that instance's world boundary. None of this instance content is
// reachable without InstanceService/TeleportService2, so it — and the onEnterWorldEvent/onKillEvent/
// onGetItemEvent/onItemUseEvent overrides and the item-drop/quest-item registrations that only
// existed to serve it — is collapsed: the statue's SETPRO13 case (var==12) removes the Greathammer
// and finishes the quest directly instead of entering the instance (same precedent as
// ishalgen._2002WheresRae's and poeta._1002RequestoftheElim's instance-detour collapses). The
// Greathammer itself, which Java grants via onEnterWorldEvent once var reaches 12, is granted
// directly in Titus's SETPRO12 case instead so it's still obtainable. Java's own register() npc list
// omits 700603 ("Hidden Library Exit"), so that dialog's var==16 teleport-out branch was already
// unreachable in Java and is dropped along with the rest of the instance content; the Hidden Switch
// (700604, var==13 useQuestObject) is likewise unreachable once var never reaches 13 and is omitted.
// Teleport-to-zone calls on Sulla/Machiah/Pyrrha's SETPRO steps (TeleportService2) are dropped —
// only the var transition survives, per the same simplification noted above.
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

namespace Quest.Inggison;

public sealed class _10023SullasStartlingDiscovery : QuestHandlerBase
{
    private const int QuestIdConst = 10023;
    private const int Yulia   = 798928;
    private const int Sulla   = 798975;
    private const int Philon  = 798981;
    private const int WesternMass  = 730226;
    private const int EasternMass  = 730227;
    private const int SouthernMass = 730228;
    private const int Machiah = 798513;
    private const int Pyrrha  = 798225;
    private const int Gelon   = 798979;
    private const int Titus   = 798990;
    private const int DrakanStatue = 730295;
    private const int GreathammerItem = 182206613;

    private readonly IItemDao _itemDao;

    public _10023SullasStartlingDiscovery(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        int[] npcs = [Yulia, Sulla, Philon, WesternMass, EasternMass, SouthernMass, Machiah, Pyrrha, Gelon, Titus, DrakanStatue];
        foreach (int npc in npcs)
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        int[] preceding = [1094, 10020];
        return DefaultOnLvlUpEventAsync(env, conn, preceding, isZoneMission: true, ct);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var         = entry.GetVar(0);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != Yulia) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        if (targetId == Yulia)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return false;
        }

        if (targetId == Sulla)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.QUEST_SELECT && var == 6)
                return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
            if (dialog == DialogAction.QUEST_SELECT && var == 9)
                return await SendQuestDialogAsync(conn, targetObjId, 4080, ct);
            if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO2)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            if (dialog == DialogAction.SETPRO7)
                return await DefaultCloseDialogAsync(env, conn, 6, 7, ct);
            if (dialog == DialogAction.SETPRO10)
                return await DefaultCloseDialogAsync(env, conn, 9, 10, ct);
            return false;
        }

        if (targetId == Philon)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 2)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO3)
                return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
            return false;
        }

        if (targetId == Machiah)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 7)
                return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
            if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO8)
                return await DefaultCloseDialogAsync(env, conn, 7, 8, ct);
            return false;
        }

        if (targetId == Pyrrha)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 8)
                return await SendQuestDialogAsync(conn, targetObjId, 3739, ct);
            if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO9)
                return await DefaultCloseDialogAsync(env, conn, 8, 9, ct);
            return false;
        }

        if (targetId == Gelon)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 10)
                return await SendQuestDialogAsync(conn, targetObjId, 1608, ct);
            if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO11)
                return await DefaultCloseDialogAsync(env, conn, 10, 11, ct);
            return false;
        }

        if (targetId == Titus)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 11)
                return await SendQuestDialogAsync(conn, targetObjId, 1949, ct);
            if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO12)
            {
                await PlayQuestMovieAsync(conn, player, 504, ct);
                // Deviation: grants the Greathammer here instead of via onEnterWorldEvent — see file header.
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 11, 12, reward: false, sameNpc: false,
                    giveItemId: GreathammerItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
            }
            return false;
        }

        if (targetId is WesternMass or EasternMass or SouthernMass)
        {
            if (dialog != DialogAction.USE_OBJECT) return false;
            int step = targetId == WesternMass ? 3 : targetId == EasternMass ? 4 : 5;
            return var == step && await UseQuestObjectAsync(env, conn, step, step + 1, false, dieObject: false, ct);
        }

        if (targetId == DrakanStatue)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 12)
                return await SendQuestDialogAsync(conn, targetObjId, 3995, ct);
            if (dialog == DialogAction.SETPRO13 && var == 12)
            {
                var hammer = player.Inventory.FindByItemId(GreathammerItem);
                if (hammer is null || hammer.Count <= 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 10001, ct);

                // Collapsed dungeon excursion straight to completion — see file header.
                await RemoveQuestItemAsync(player, conn, _itemDao, GreathammerItem, 1, ct);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            return false;
        }

        return false;
    }
}
