// Port of Java data/scripts/system/handlers/quest/verteron/_1183SpiritOfNature.java
// (Balthazar). Talk to the first Spirit (730012) to start; use the second (730013, var 0->1,
// gives item 182200550); talk to the third (730014, var 1->2, gives item 182200565); turn in at
// the first spirit (removes both items, flips to REWARD).
using System.Linq;
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

namespace Quest.Verteron;

public sealed class _1183SpiritOfNature : QuestHandlerBase
{
    private const int QuestIdConst  = 1183;
    private const int SpiritOneObj  = 730012;
    private const int SpiritTwoObj  = 730013;
    private const int SpiritThreeObj = 730014;
    private const int TwigItemId    = 182200550;
    private const int LeafItemId    = 182200565;

    private readonly IItemDao _itemDao;

    public _1183SpiritOfNature(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(SpiritOneObj).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(SpiritOneObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SpiritTwoObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SpiritThreeObj).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        var entry = player.Quests.Get(QuestId);
        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry is null)
        {
            if (targetId == SpiritOneObj)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == SpiritTwoObj)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    if (player.Inventory.FindByItemId(TwigItemId) is null
                        && !await GiveQuestItemAsync(player, conn, _itemDao, TwigItemId, 1, ct))
                        return true;
                    entry.SetVar(0, entry.GetVar(0) + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
                return false;
            }
            if (targetId == SpiritThreeObj)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    if (player.Inventory.FindByItemId(LeafItemId) is null
                        && !await GiveQuestItemAsync(player, conn, _itemDao, LeafItemId, 1, ct))
                        return true;
                    entry.SetVar(0, entry.GetVar(0) + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            if (targetId == SpiritOneObj)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    entry.SetVar(0, 3);
                    await RemoveQuestItemAsync(player, conn, _itemDao, TwigItemId, 1, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, LeafItemId, 1, ct);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestEndDialogAsync(env, conn, ct);
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == SpiritOneObj)
        {
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
