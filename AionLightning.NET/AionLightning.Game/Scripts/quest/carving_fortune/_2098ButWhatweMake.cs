// Port of Java data/scripts/system/handlers/quest/carving_fortune/_2098ButWhatweMake.java (Manu72).
// Long relay chain across 15 NPCs (var 0 through 14): Munin (203550, start + var0->1, gives item
// 182207089) -> Hreidmar(1->2) -> Bulagan(2->3) -> Cayron(3->4) -> Vanargand(4->5) -> Esnu(5->6,
// swaps 182207089 for 182207090) -> Skuld(6->7) -> Ananta(7->8) -> Seznec(8->9) -> Kasir(9->10,
// swaps 182207090 for 182207091) -> Aegir(10->11) -> Heintz(11->12) -> Delris(12->13) ->
// Votan(13->14) -> Kvasir(14 -> REWARD, swaps 182207091 for 182207092), turn in back at Munin.
// Level-up gated on 2097. Java bug preserved as-is (not a bug, deliberate): every give/removeQuestItem
// call along this chain ignores its own success/failure (Java's `if (giveQuestItem(...));` no-op
// statement) - the relay still advances even if the player's bag is full, so this port mirrors that
// by not gating step advancement on GiveQuestItemAsync's result either.
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

namespace Quest.CarvingFortune;

public sealed class _2098ButWhatweMake : QuestHandlerBase
{
    private const int QuestIdConst = 2098;
    private const int MuninNpc     = 203550;
    private const int HreidmarNpc  = 204361;
    private const int BulaganNpc   = 204408;
    private const int CayronNpc    = 205198;
    private const int VanargandNpc = 204805;
    private const int EsnuNpc      = 204808;
    private const int SkuldNpc     = 203546;
    private const int AnantaNpc    = 204387;
    private const int SeznecNpc    = 205190;
    private const int KasirNpc     = 204207;
    private const int AegirNpc     = 204301;
    private const int HeintzNpc    = 205155;
    private const int DelrisNpc    = 204784;
    private const int VotanNpc     = 278001;
    private const int KvasirNpc    = 204053;

    private const int TokenA = 182207089;
    private const int TokenB = 182207090;
    private const int TokenC = 182207091;
    private const int TokenD = 182207092;

    private readonly IItemDao _itemDao;

    public _2098ButWhatweMake(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(MuninNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(MuninNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HreidmarNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BulaganNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(CayronNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(VanargandNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EsnuNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SkuldNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AnantaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SeznecNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KasirNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AegirNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HeintzNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(DelrisNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(VotanNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KvasirNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 2097, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (targetId == MuninNpc)
        {
            if (entry is null || entry.Status == QuestStatus.START)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (entry is not null && dialog == DialogAction.SETPRO1)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, entry.GetVar(0) + 1, toReward: false, ct);
                    bool sent = await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    await GiveQuestItemAsync(player, conn, _itemDao, TokenA, 1, ct);
                    return sent;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }

            if (entry.Status == QuestStatus.REWARD)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 14, toReward: true, ct);
                    return await SendQuestEndDialogAsync(env, conn, ct);
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }

            return false;
        }

        if (entry is null || entry.Status != QuestStatus.START) return false;

        return targetId switch
        {
            HreidmarNpc  => await HandleWaypointAsync(env, conn, entry, targetObjId, dialog, 1, DialogAction.SETPRO2, 1352, ct),
            BulaganNpc   => await HandleWaypointAsync(env, conn, entry, targetObjId, dialog, 2, DialogAction.SETPRO3, 1693, ct),
            CayronNpc    => await HandleWaypointAsync(env, conn, entry, targetObjId, dialog, 3, DialogAction.SETPRO4, 2034, ct),
            VanargandNpc => await HandleWaypointAsync(env, conn, entry, targetObjId, dialog, 4, DialogAction.SETPRO5, 2375, ct),
            EsnuNpc      => await HandleItemWaypointAsync(env, conn, entry, targetObjId, dialog, 5, DialogAction.SETPRO6, 2716, TokenA, TokenB, ct),
            SkuldNpc     => await HandleWaypointAsync(env, conn, entry, targetObjId, dialog, 6, DialogAction.SETPRO7, 3057, ct),
            AnantaNpc    => await HandleWaypointAsync(env, conn, entry, targetObjId, dialog, 7, DialogAction.SETPRO8, 3398, ct),
            SeznecNpc    => await HandleWaypointAsync(env, conn, entry, targetObjId, dialog, 8, DialogAction.SETPRO9, 3739, ct),
            KasirNpc     => await HandleItemWaypointAsync(env, conn, entry, targetObjId, dialog, 9, DialogAction.SETPRO10, 4080, TokenB, TokenC, ct),
            AegirNpc     => await HandleWaypointAsync(env, conn, entry, targetObjId, dialog, 10, DialogAction.SETPRO11, 1608, ct),
            HeintzNpc    => await HandleWaypointAsync(env, conn, entry, targetObjId, dialog, 11, DialogAction.SETPRO12, 1949, ct),
            DelrisNpc    => await HandleWaypointAsync(env, conn, entry, targetObjId, dialog, 12, DialogAction.SETPRO13, 2290, ct),
            VotanNpc     => await HandleWaypointAsync(env, conn, entry, targetObjId, dialog, 13, DialogAction.SETPRO14, 2631, ct),
            KvasirNpc    => await HandleFinalAsync(env, conn, entry, targetObjId, dialog, ct),
            _ => false,
        };
    }

    /// <summary>Plain relay step (no item exchange): show the preview page at <paramref name="requiredVar"/>, or advance on <paramref name="advanceAction"/>.</summary>
    private async ValueTask<bool> HandleWaypointAsync(QuestEnv env, GsClientConnection conn, QuestEntry entry, int targetObjId,
        DialogAction dialog, int requiredVar, DialogAction advanceAction, int questSelectDialogId, CancellationToken ct)
    {
        if (entry.GetVar(0) != requiredVar) return false;
        if (dialog == DialogAction.QUEST_SELECT)
            return await SendQuestDialogAsync(conn, targetObjId, questSelectDialogId, ct);
        if (dialog == advanceAction)
            return await DefaultCloseDialogAsync(env, conn, requiredVar, requiredVar + 1, ct);
        return await SendQuestStartDialogAsync(env, conn, ct);
    }

    /// <summary>Relay step that swaps a carried token item for the next one on advance (give/remove results ignored, per the header note).</summary>
    private async ValueTask<bool> HandleItemWaypointAsync(QuestEnv env, GsClientConnection conn, QuestEntry entry, int targetObjId,
        DialogAction dialog, int requiredVar, DialogAction advanceAction, int questSelectDialogId,
        int removeItemId, int giveItemId, CancellationToken ct)
    {
        if (entry.GetVar(0) != requiredVar) return false;
        if (dialog == DialogAction.QUEST_SELECT)
            return await SendQuestDialogAsync(conn, targetObjId, questSelectDialogId, ct);
        if (dialog == advanceAction)
        {
            await RemoveQuestItemAsync(env.Player, conn, _itemDao, removeItemId, 1, ct);
            await ChangeQuestStepAsync(conn, entry, 0, requiredVar + 1, toReward: false, ct);
            bool sent = await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            await GiveQuestItemAsync(env.Player, conn, _itemDao, giveItemId, 1, ct);
            return sent;
        }
        return await SendQuestStartDialogAsync(env, conn, ct);
    }

    /// <summary>Final relay step at Kvasir (var 14): swaps the last token for the finishing item and flips to REWARD.</summary>
    private async ValueTask<bool> HandleFinalAsync(QuestEnv env, GsClientConnection conn, QuestEntry entry, int targetObjId, DialogAction dialog, CancellationToken ct)
    {
        if (entry.GetVar(0) != 14) return false;
        if (dialog == DialogAction.QUEST_SELECT)
            return await SendQuestDialogAsync(conn, targetObjId, 2972, ct);
        if (dialog == DialogAction.SET_SUCCEED)
        {
            await RemoveQuestItemAsync(env.Player, conn, _itemDao, TokenC, 1, ct);
            await ChangeQuestStepAsync(conn, entry, -1, 0, toReward: true, ct);
            bool sent = await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            await GiveQuestItemAsync(env.Player, conn, _itemDao, TokenD, 1, ct);
            return sent;
        }
        return await SendQuestStartDialogAsync(env, conn, ct);
    }
}
