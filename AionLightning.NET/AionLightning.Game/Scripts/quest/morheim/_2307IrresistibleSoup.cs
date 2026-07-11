// Port of Java data/scripts/system/handlers/quest/morheim/_2307IrresistibleSoup.java.
// Start at Favyr (204378); smelling the Aromatic Soup object (700247, var 0->1, no dialog shown);
// at Spedor (204336, var 1) pick one of three soup recipes (SETPRO1/SELECT_ACTION_1182/1267),
// each consuming a different quest item and flipping straight to REWARD; turn in back at Favyr.
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

namespace Quest.Morheim;

public sealed class _2307IrresistibleSoup : QuestHandlerBase
{
    private const int QuestIdConst = 2307;
    private const int FavyrNpc     = 204378;
    private const int SpedorNpc    = 204336;
    private const int SoupObj      = 700247;

    private readonly IItemDao _itemDao;

    public _2307IrresistibleSoup(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(FavyrNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(FavyrNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SpedorNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SoupObj).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == FavyrNpc)
        {
            if (entry is null)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.REWARD)
                return await SendQuestEndDialogAsync(env, conn, ct);
            return false;
        }

        if (targetId == SpedorNpc && entry is { Status: QuestStatus.START } && entry.GetVar(0) == 1)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);

            int? removeItemId = dialog switch
            {
                DialogAction.SETPRO1 => 182204106,
                DialogAction.SELECT_ACTION_1182 => 182204107,
                DialogAction.SELECT_ACTION_1267 => 182204108,
                _ => null,
            };
            if (removeItemId is null)
                return await SendQuestStartDialogAsync(env, conn, ct);

            entry.SetVar(0, dialog == DialogAction.SETPRO1 ? 2 : 1);
            entry.Status = QuestStatus.REWARD;
            await UpdateQuestStatusAsync(conn, entry, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, removeItemId.Value, 1, ct);
            await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
            return true;
        }

        if (targetId == SoupObj && entry is { Status: QuestStatus.START } && entry.GetVar(0) == 0
            && dialog == DialogAction.USE_OBJECT)
        {
            entry.SetVar(0, 1);
            await UpdateQuestStatusAsync(conn, entry, ct);
        }
        return false;
    }
}
