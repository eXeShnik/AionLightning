// Port of Java data/scripts/system/handlers/quest/heiron/_3200PriceOfGoodwill.java (kecimis).
// Accept at Roikinerk (204658); his SETPRO1 creates a Steel Rake instance (300100000) and teleports
// the player in (403.55, 508.11, 885.77), advancing var0. Talk Haorunerk (798332, var1 -> movie 431 /
// SETPRO2 var2), loot Haorunerks Bag (700522, var2 -> var3 + relocation), report Garkbinerk (279006,
// var3 -> REWARD); turn in at Kuruminerk (798322).
// Skip vs Java: the Haorunerks-Bag TeleportService2.teleportTo(400010000, ...) is a non-entry
// relocation - dropped with a note, the var2->var3 state transition is kept (established precedent).
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

namespace Quest.Heiron;

public sealed class _3200PriceOfGoodwill : QuestHandlerBase
{
    private const int QuestIdConst = 3200;
    private const int Roikinerk    = 204658;
    private const int Haorunerk    = 798332;
    private const int HaorunerksBag = 700522;
    private const int Garkbinerk   = 279006;
    private const int Kuruminerk   = 798322;
    private const int TeleportScroll = 182209082;
    private const int SteelRakeWorld = 300100000;

    public _3200PriceOfGoodwill(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(Roikinerk).OnQuestStart.Add(QuestId);
        engine.RegisterQuestItem(TeleportScroll, QuestId);
        foreach (int npc in new[] { Roikinerk, Haorunerk, HaorunerksBag, Garkbinerk, Kuruminerk })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == Roikinerk)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == Kuruminerk)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == Roikinerk)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
                if (dialog == DialogAction.SELECT_ACTION_1011)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    await EnterInstanceAsync(player, conn, SteelRakeWorld, 403.55f, 508.11f, 885.77f, 0, ct);
                    entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return true;
                }
                return false;
            }
            if (targetId == Haorunerk && var == 1)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SELECT_ACTION_1353)
                {
                    await PlayQuestMovieAsync(conn, player, 431, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO2)
                {
                    entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
                return false;
            }
            if (targetId == HaorunerksBag && var == 2)
            {
                entry.SetVar(0, var + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                // note: Java TeleportService2.teleportTo(400010000, 3419.16, 2445.43, 2766.54, 57) - non-entry relocation, dropped.
                return false;
            }
            if (targetId == Garkbinerk && var == 3)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                {
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return true;
                }
                return false;
            }
        }
        return false;
    }
}
