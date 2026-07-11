// Port of Java data/scripts/system/handlers/quest/verteron/_1158VillageSealFound.java
// (Rhys2002, rework zhkchi). Talk to Gaphyrk (798003) to start; use Item Stack (700003) to give
// the Seal item and flip straight to REWARD; turn in at Santenius (203128).
using System.Linq;
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

namespace Quest.Verteron;

public sealed class _1158VillageSealFound : QuestHandlerBase
{
    private const int QuestIdConst  = 1158;
    private const int GaphyrkNpc    = 798003;
    private const int ItemStackObj  = 700003;
    private const int SanteniusNpc  = 203128;
    private const int SealItemId    = 182200502;

    private readonly IItemDao _itemDao;

    public _1158VillageSealFound(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(GaphyrkNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(GaphyrkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ItemStackObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SanteniusNpc).OnTalk.Add(QuestId);
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
            if (targetId == GaphyrkNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        int var = entry.GetVar(0);
        if (entry.Status == QuestStatus.START)
        {
            if (targetId == ItemStackObj && var == 0)
            {
                switch (dialog)
                {
                    case DialogAction.USE_OBJECT:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.SELECT_ACTION_1353:
                        return await SendQuestDialogAsync(conn, targetObjId, 1353, ct);
                    case DialogAction.SETPRO1:
                        if (!await GiveQuestItemAsync(player, conn, _itemDao, SealItemId, 1, ct))
                            return true;
                        entry.SetVar(0, 1);
                        entry.Status = QuestStatus.REWARD;
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await CloseDialogWindowAsync(conn, targetObjId, ct);
                    default:
                        return false;
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == SanteniusNpc)
        {
            switch (dialog)
            {
                case DialogAction.USE_OBJECT:
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                case DialogAction.SELECT_QUEST_REWARD:
                    await RemoveQuestItemAsync(player, conn, _itemDao, SealItemId, 1, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                default:
                    return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }
}
