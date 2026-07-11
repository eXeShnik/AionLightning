// Port of Java data/scripts/system/handlers/quest/heiron/_18600ScoringSomeBadStigma.java.
// Report-to-quest start: Perento (204500) gives a paper voucher (182213000); hand it to
// Koruchinerk (798321, var 0->1); go meet Herthia (205228, var 1->3, REWARD); bring the Fake
// Stigma (182213001) back to Perento. Skip vs Java: qs.canRepeat() (daily-repeat/cooldown) isn't
// ported, so only a first-time run is offered — still fully completable once.
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

public sealed class _18600ScoringSomeBadStigma : QuestHandlerBase
{
    private const int QuestIdConst  = 18600;
    private const int PerentoNpc    = 204500;
    private const int KoruchinerkNpc = 798321;
    private const int HerthiaNpc    = 205228;
    private const int VoucherItemId = 182213000;
    private const int FakeStigmaId  = 182213001;

    private readonly IItemDao _itemDao;

    public _18600ScoringSomeBadStigma(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(PerentoNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(PerentoNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KoruchinerkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HerthiaNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == PerentoNpc)
        {
            if (entry is null || entry.Status == QuestStatus.NONE)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.QUEST_ACCEPT_1)
                {
                    if (await StartMissionAsync(conn, player, QuestStatus.START, ct))
                    {
                        await GiveQuestItemAsync(player, conn, _itemDao, VoucherItemId, 1, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
                    }
                    return false;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.REWARD)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, FakeStigmaId, 1, ct);
                    return await SendQuestEndDialogAsync(env, conn, ct);
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        else if (targetId == KoruchinerkNpc)
        {
            if (entry is not null && entry.Status == QuestStatus.START)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 0, 1, reward: false, sameNpc: false,
                        giveItemId: 0, giveItemCount: 0, removeItemId: VoucherItemId, removeItemCount: 1, ct);
            }
        }
        else if (targetId == HerthiaNpc)
        {
            if (entry is not null && entry.Status == QuestStatus.START && entry.GetVar(0) == 1)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    entry.SetVar(0, 3);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
            }
        }
        return false;
    }
}
