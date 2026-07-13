// Port of Java data/scripts/system/handlers/quest/miragent_holy_templar/_3934TheQuestForTemplars.java
// (Nanou). Start at Lavirintos (203701); an 8-npc relay chain (798359->798360->798361->798362->
// 798363->798364->798365->798366, var 0->8); report to Jucleas (203752) with the Oath Stone
// (186000080, removed) to flip to reward; turn in at Lavirintos.
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

namespace Quest.MiragentHolyTemplar;

public sealed class _3934TheQuestForTemplars : QuestHandlerBase
{
    private const int QuestIdConst  = 3934;
    private const int LavirintosNpc = 203701;
    private const int NianaloNpc    = 798359;
    private const int NavidNpc      = 798360;
    private const int PavelNpc      = 798361;
    private const int PendaonNpc    = 798362;
    private const int PoeviusNpc    = 798363;
    private const int BelicanonNpc  = 798364;
    private const int MahelnuNpc    = 798365;
    private const int PaterNpc      = 798366;
    private const int JucleasNpc    = 203752;
    private const int OathStoneItem = 186000080;

    private readonly IItemDao _itemDao;

    public _3934TheQuestForTemplars(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(LavirintosNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(LavirintosNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(NianaloNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(NavidNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(PavelNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(PendaonNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(PoeviusNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BelicanonNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MahelnuNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(PaterNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(JucleasNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != LavirintosNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == NianaloNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == NavidNpc)
            {
                if (var != 1) return false;
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == PavelNpc)
            {
                if (var != 2) return false;
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3)
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                return false;
            }
            if (targetId == PendaonNpc)
            {
                if (var != 3) return false;
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO4)
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                return false;
            }
            if (targetId == PoeviusNpc)
            {
                if (var != 4) return false;
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO5)
                    return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                return false;
            }
            if (targetId == BelicanonNpc)
            {
                if (var != 5) return false;
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (dialog == DialogAction.SETPRO6)
                    return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
                return false;
            }
            if (targetId == MahelnuNpc)
            {
                if (var != 6) return false;
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                if (dialog == DialogAction.SETPRO7)
                    return await DefaultCloseDialogAsync(env, conn, 6, 7, ct);
                return false;
            }
            if (targetId == PaterNpc)
            {
                if (var != 7) return false;
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                if (dialog == DialogAction.SETPRO8)
                    return await DefaultCloseDialogAsync(env, conn, 7, 8, ct);
                return false;
            }
            if (targetId == JucleasNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        if (var == 8)
                            return await SendQuestDialogAsync(conn, targetObjId, 3739, ct);
                        return false;
                    case DialogAction.SET_SUCCEED:
                        if (HasItem(player, OathStoneItem, 1))
                        {
                            await RemoveQuestItemAsync(player, conn, _itemDao, OathStoneItem, 1, ct);
                            return await DefaultCloseDialogAsync(env, conn, 8, 8, reward: true, sameNpc: false, ct);
                        }
                        return await SendQuestDialogAsync(conn, targetObjId, 3825, ct);
                    case DialogAction.FINISH_DIALOG:
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    default:
                        return false;
                }
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD && targetId == LavirintosNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    private static bool HasItem(Player player, int itemId, long count)
    {
        var item = player.Inventory.FindByItemId(itemId);
        return item is not null && item.Count >= count;
    }
}
