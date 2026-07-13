// Port of Java data/scripts/system/handlers/quest/theobomos/_1093TheCalydonRuins.java.
// Zone-mission quest chained off 1092: report to Atropos (798155, var 0->1, teleports away),
// Hestia (798176->798155 typo aside, actually 203784, var 1->2, grants the Calydon Candy
// 182208013), Jamanok (798176, var 2->3, plays movie 365); eating the candy (item-use) advances
// var 3->4; rub all three stone plates (700391/700392/700393, var 4->5->6->7, each granting a
// rubbing copy item); Serimnir (798212) validates the three rubbings (var 7->8) then flips to
// REWARD (consuming the candy); turn in at Atropos.
// Skip vs Java: Atropos' SETPRO1 calls TeleportService2.teleportTo - omitted, no TeleportService2
// in this port (same precedent as quest/eltnen/_1482ATeleportationAdventure.cs); the var/status
// transition is kept. The three stone-plate handlers return false even on success in the Java
// source (no explicit ack after the var/item change) - reproduced as-is.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Theobomos;

public sealed class _1093TheCalydonRuins : QuestHandlerBase
{
    private const int QuestIdConst  = 1093;
    private const int AtroposNpc    = 798155;
    private const int HestiaNpc     = 203784;
    private const int JamanokNpc    = 798176;
    private const int StonePlate1   = 700391;
    private const int StonePlate2   = 700392;
    private const int StonePlate3   = 700393;
    private const int SerimnirNpc   = 798212;
    private const int CalydonCandyItem   = 182208013;
    private const int FirstRubbedCopy    = 182208014;
    private const int SecondRubbedCopy   = 182208015;
    private const int ThirdRubbedCopy    = 182208016;

    private readonly IItemDao _itemDao;

    public _1093TheCalydonRuins(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestItem(CalydonCandyItem, QuestId);
        engine.RegisterQuestNpc(AtroposNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HestiaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(JamanokNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StonePlate1).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StonePlate2).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StonePlate3).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SerimnirNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1091, isZoneMission: true, ct);

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != CalydonCandyItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.GetVar(0) != 3) return false;

        entry.SetVar(0, entry.GetVar(0) + 1);
        await UpdateQuestStatusAsync(conn, entry, ct);
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

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != AtroposNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        if (targetId == AtroposNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1 && var == 0)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return false;
        }

        if (targetId == HestiaNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO2 && var == 1)
            {
                if (!await GiveQuestItemAsync(player, conn, _itemDao, CalydonCandyItem, 1, ct))
                    return true;
                entry.SetVar(0, var + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
            return false;
        }

        if (targetId == JamanokNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                await PlayQuestMovieAsync(conn, player, 365, ct);
                return false;
            }
            if (dialog == DialogAction.SELECT_ACTION_1694)
            {
                await PlayQuestMovieAsync(conn, player, 365, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO3 && var == 2)
            {
                entry.SetVar(0, var + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
            return false;
        }

        if (targetId == SerimnirNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 7) return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                if (var == 8) return await SendQuestDialogAsync(conn, targetObjId, 3739, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO4 && var == 7)
            {
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
            if (dialog == DialogAction.SET_SUCCEED && var == 8)
            {
                entry.Status = QuestStatus.REWARD;
                await RemoveQuestItemAsync(player, conn, _itemDao, CalydonCandyItem, 1, ct);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM && var == 7)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 7, 8, false, 3739, 10001, ct);
            return false;
        }

        if (targetId == StonePlate1)
        {
            if (dialog == DialogAction.USE_OBJECT && var == 4)
            {
                if (!await GiveQuestItemAsync(player, conn, _itemDao, FirstRubbedCopy, 1, ct)) return false;
                entry.SetVar(0, 5);
                await UpdateQuestStatusAsync(conn, entry, ct);
            }
            return false;
        }

        if (targetId == StonePlate2)
        {
            if (dialog == DialogAction.USE_OBJECT && var == 5)
            {
                if (!await GiveQuestItemAsync(player, conn, _itemDao, SecondRubbedCopy, 1, ct)) return false;
                entry.SetVar(0, 6);
                await UpdateQuestStatusAsync(conn, entry, ct);
            }
            return false;
        }

        if (targetId == StonePlate3)
        {
            if (dialog == DialogAction.USE_OBJECT && var == 6)
            {
                if (!await GiveQuestItemAsync(player, conn, _itemDao, ThirdRubbedCopy, 1, ct)) return false;
                entry.SetVar(0, 7);
                await UpdateQuestStatusAsync(conn, entry, ct);
            }
            return false;
        }

        return false;
    }
}
