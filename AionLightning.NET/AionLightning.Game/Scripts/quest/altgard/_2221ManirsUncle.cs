// Port of Java data/scripts/system/handlers/quest/altgard/_2221ManirsUncle.java (MrPoke/Gigi).
// Talk to Manir (203607), talk to Groken (203608), pick up a key item, use Groken's Safe
// (700214), report back and turn in.
using System.Linq;
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

namespace Quest.Altgard;

public sealed class _2221ManirsUncle : QuestHandlerBase
{
    private const int QuestIdConst = 2221;
    private const int ManirNpc     = 203607;
    private const int GrokenNpc    = 203608;
    private const int SafeObj      = 700214;
    private const int KeyItemId    = 182203215;

    private readonly IItemDao _itemDao;

    public _2221ManirsUncle(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(ManirNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(ManirNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GrokenNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SafeObj).OnTalk.Add(QuestId);
        engine.RegisterItemGet(KeyItemId, QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == ManirNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            switch (targetId)
            {
                case GrokenNpc:
                    if (dialog == DialogAction.QUEST_SELECT && var == 0)
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (dialog == DialogAction.QUEST_SELECT && var == 2)
                        return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    if (dialog == DialogAction.SETPRO1)
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                    if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                    {
                        await RemoveQuestItemAsync(player, conn, _itemDao, KeyItemId, 1, ct);
                        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: true, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                    }
                    return false;
                case SafeObj:
                    if (dialog == DialogAction.USE_OBJECT && var == 1)
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (dialog == DialogAction.SETPRO2)
                        return await CloseDialogWindowAsync(conn, targetObjId, ct);
                    return false;
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == GrokenNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override async ValueTask<bool> OnItemGetAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != KeyItemId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
        return true;
    }
}
