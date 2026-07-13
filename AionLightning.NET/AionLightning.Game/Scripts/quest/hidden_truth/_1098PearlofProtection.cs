// Port of Java data/scripts/system/handlers/quest/hidden_truth/_1098PearlofProtection.java
// (Manu72; reworked vlog). 15-npc "blessing/energy" relay chain (var 0->14), each step advancing
// via a SETPROn dialog at the next npc in line; three steps also give/remove an intermediate quest
// item (182206062-182206065). Final step (Maximus, SET_SUCCEED) flips to REWARD; turn in at Pernos
// (removes 182206065 on QUEST_SELECT, matching Java's literal ordering — the REWARD branch is
// checked before START, though functionally either order works since qs.getStatus() partitions the
// two cases).
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

namespace Quest.HiddenTruth;

public sealed class _1098PearlofProtection : QuestHandlerBase
{
    private const int QuestIdConst = 1098;

    private const int PernosNpc    = 790001;
    private const int DaminuNpc    = 730008;
    private const int LodasNpc     = 730019;
    private const int ArboluNpc    = 204647;
    private const int KhidiaNpc    = 203183;
    private const int TumblusenNpc = 203989;
    private const int AtroposNpc   = 798155;
    private const int AphesiusNpc  = 204549;
    private const int JucleasNpc   = 203752;
    private const int MoraiNpc     = 203164;
    private const int GaiaNpc      = 203917;
    private const int KimeiaNpc    = 203996;
    private const int JamanokNpc   = 798176;
    private const int SerimnirNpc  = 798212;
    private const int MaximusNpc   = 204535;

    private const int Item1 = 182206062;
    private const int Item2 = 182206063;
    private const int Item3 = 182206064;
    private const int Item4 = 182206065;

    private readonly IItemDao _itemDao;

    public _1098PearlofProtection(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        int[] npcs =
        [
            PernosNpc, DaminuNpc, LodasNpc, ArboluNpc, KhidiaNpc, TumblusenNpc, AtroposNpc,
            AphesiusNpc, JucleasNpc, MoraiNpc, GaiaNpc, KimeiaNpc, JamanokNpc, SerimnirNpc, MaximusNpc,
        ];
        foreach (int npc in npcs)
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1097, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var         = entry.GetVar(0);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != PernosNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, Item4, 1, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        if (entry.Status != QuestStatus.START) return false;

        if (targetId == PernosNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 0, 1, reward: false, sameNpc: false,
                    giveItemId: Item1, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
        }
        else if (targetId == DaminuNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO2)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
        }
        else if (targetId == LodasNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 2)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SETPRO3)
                return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
        }
        else if (targetId == ArboluNpc)
        {
            if ((dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.USE_OBJECT) && var == 3)
                return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            if (dialog == DialogAction.SETPRO4)
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 3, 4, reward: false, sameNpc: false,
                    giveItemId: Item2, giveItemCount: 1, removeItemId: Item1, removeItemCount: 1, ct);
        }
        else if (targetId == KhidiaNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 4)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SETPRO5)
                return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
        }
        else if (targetId == TumblusenNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 5)
                return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
            if (dialog == DialogAction.SETPRO6)
                return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
        }
        else if (targetId == AtroposNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 6)
                return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
            if (dialog == DialogAction.SETPRO7)
                return await DefaultCloseDialogAsync(env, conn, 6, 7, ct);
        }
        else if (targetId == AphesiusNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 7)
                return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
            if (dialog == DialogAction.SETPRO8)
                return await DefaultCloseDialogAsync(env, conn, 7, 8, ct);
        }
        else if (targetId == JucleasNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 8)
                return await SendQuestDialogAsync(conn, targetObjId, 3739, ct);
            if (dialog == DialogAction.SETPRO9)
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 8, 9, reward: false, sameNpc: false,
                    giveItemId: Item3, giveItemCount: 1, removeItemId: Item2, removeItemCount: 1, ct);
        }
        else if (targetId == MoraiNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 9)
                return await SendQuestDialogAsync(conn, targetObjId, 4080, ct);
            if (dialog == DialogAction.SETPRO10)
                return await DefaultCloseDialogAsync(env, conn, 9, 10, ct);
        }
        else if (targetId == GaiaNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 10)
                return await SendQuestDialogAsync(conn, targetObjId, 1608, ct);
            if (dialog == DialogAction.SETPRO11)
                return await DefaultCloseDialogAsync(env, conn, 10, 11, ct);
        }
        else if (targetId == KimeiaNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 11)
                return await SendQuestDialogAsync(conn, targetObjId, 1949, ct);
            if (dialog == DialogAction.SETPRO12)
                return await DefaultCloseDialogAsync(env, conn, 11, 12, ct);
        }
        else if (targetId == JamanokNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 12)
                return await SendQuestDialogAsync(conn, targetObjId, 2290, ct);
            if (dialog == DialogAction.SETPRO13)
                return await DefaultCloseDialogAsync(env, conn, 12, 13, ct);
        }
        else if (targetId == SerimnirNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 13)
                return await SendQuestDialogAsync(conn, targetObjId, 2631, ct);
            if (dialog == DialogAction.SETPRO14)
                return await DefaultCloseDialogAsync(env, conn, 13, 14, ct);
        }
        else if (targetId == MaximusNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 14)
                return await SendQuestDialogAsync(conn, targetObjId, 2972, ct);
            if (dialog == DialogAction.SET_SUCCEED)
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 14, 14, reward: true, sameNpc: false,
                    giveItemId: Item4, giveItemCount: 1, removeItemId: Item3, removeItemCount: 1, ct);
        }

        return false;
    }
}
