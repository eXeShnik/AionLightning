// Port of Java data/scripts/system/handlers/quest/brusthonin/_4042MessageinaBottle.java.
// Interact with the Bottle (730150) to receive the sealed letter (182209024, no quest state yet);
// use the item to open the start dialog; talk to Sahnu (205192, var 0->1, exchanges the bottle for
// its contents 182209025); deliver to Gunter (204225, var 1->2, contents consumed); return to
// Sahnu (var 2 -> REWARD).
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

namespace Quest.Brusthonin;

public sealed class _4042MessageinaBottle : QuestHandlerBase
{
    private const int QuestIdConst  = 4042;
    private const int BottleObj     = 730150;
    private const int SahnuNpc      = 205192;
    private const int GunterNpc     = 204225;
    private const int BottleItemId  = 182209024;
    private const int LetterItemId  = 182209025;

    private readonly IItemDao _itemDao;

    public _4042MessageinaBottle(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(BottleItemId, QuestId);
        engine.RegisterQuestNpc(BottleObj).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(BottleObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SahnuNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GunterNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != BottleItemId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is not null) return false;

        await conn.SendAsync(new SM_DIALOG_WINDOW(0, 4, QuestId), ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId == 0 && env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            if (targetId == BottleObj)
                return await GiveQuestItemAsync(player, conn, _itemDao, BottleItemId, 1, ct);
            return false;
        }

        if (targetId == SahnuNpc)
        {
            if (entry.Status == QuestStatus.START && entry.GetVar(0) == 0)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, LetterItemId, 1, ct))
                        return true;
                    await RemoveQuestItemAsync(player, conn, _itemDao, BottleItemId, 1, ct);
                    entry.SetVar(0, entry.GetVar(0) + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.START && entry.GetVar(0) == 2)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.REWARD)
                return await SendQuestEndDialogAsync(env, conn, ct);
            return false;
        }

        if (targetId == GunterNpc)
        {
            if (entry.Status == QuestStatus.START && entry.GetVar(0) == 1)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, LetterItemId, 1, ct);
                    entry.SetVar(0, entry.GetVar(0) + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        return false;
    }
}
