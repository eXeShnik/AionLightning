// Port of Java data/scripts/system/handlers/quest/pandaemonium/_4969GrowthNinissFourthCharm.java.
// Accept default at Ninis (798385); Maochinicherk (798068) gives the Charm Certificate
// (182207139); back at Ninis, handing in the Brilliant Aether Paper (186000093) plus 90000 kinah
// flips to REWARD; turn in at Ninis. Same dead-fallthrough simplification as
// _4966GrowthNinissFirstCharm.cs.
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

namespace Quest.Pandaemonium;

public sealed class _4969GrowthNinissFourthCharm : QuestHandlerBase
{
    private const int QuestIdConst = 4969;
    private const int NinisNpc = 798385;
    private const int MaochinicherkNpc = 798068;
    private const int CertificateItem = 182207139;
    private const int AetherPaperItem = 186000093;
    private const long KinahCost = 90000;
    private const int KinahItemId = 182400001;

    private readonly IItemDao _itemDao;

    public _4969GrowthNinissFourthCharm(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(NinisNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(NinisNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MaochinicherkNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != NinisNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
            return targetId == NinisNpc && await SendQuestEndDialogAsync(env, conn, ct);
        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);

        if (targetId == MaochinicherkNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO1 || (dialog == DialogAction.QUEST_SELECT && var != 0))
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 0, 1, reward: false, sameNpc: false,
                    giveItemId: CertificateItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
            return false;
        }

        if (targetId == NinisNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, CertificateItem, 1, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            }
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM || (dialog == DialogAction.QUEST_SELECT && var != 1))
            {
                long paperCount = player.Inventory.FindByItemId(AetherPaperItem)?.Count ?? 0;
                if (var == 1 && await TryDeductKinahAsync(player, conn, KinahCost, ct) && paperCount >= 1)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, AetherPaperItem, 1, ct);
                    await ChangeQuestStepAsync(conn, entry, 1, 1, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
            }
            if (dialog == DialogAction.FINISH_DIALOG)
                return await DefaultCloseDialogAsync(env, conn, 1, 1, ct);
            return false;
        }
        return false;
    }

    /// <summary>Java Inventory.tryDecreaseKinah(amount): deducts kinah if the player has enough, persisting the change.</summary>
    private async ValueTask<bool> TryDeductKinahAsync(Player player, GsClientConnection conn, long amount, CancellationToken ct)
    {
        var kinah = player.Inventory.FindByItemId(KinahItemId);
        if (kinah is null || kinah.Count < amount) return false;

        kinah.Count -= amount;
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinah]), ct);
        return true;
    }
}
