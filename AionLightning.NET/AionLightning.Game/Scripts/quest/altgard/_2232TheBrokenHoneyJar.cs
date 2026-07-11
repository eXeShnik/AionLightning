// Port of Java data/scripts/system/handlers/quest/altgard/_2232TheBrokenHoneyJar.java
// (MrPoke/Nephis/vlog). Talk to Gilungk (203613), relay through Tatural (203622), loot the
// Beehive (700061), collect-check, turn in.
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

namespace Quest.Altgard;

public sealed class _2232TheBrokenHoneyJar : QuestHandlerBase
{
    private const int QuestIdConst = 2232;
    private const int GilungkNpc   = 203613;
    private const int TaturalNpc   = 203622;
    private const int BeehiveObj   = 700061;

    private readonly IItemDao _itemDao;

    public _2232TheBrokenHoneyJar(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(GilungkNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(GilungkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TaturalNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BeehiveObj).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == GilungkNpc)
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
            if (targetId == GilungkNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 1:
                        return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                        return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 1, reward: true, checkOkId: 5, checkFailId: 2716, ct);
                    case DialogAction.FINISH_DIALOG:
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    default:
                        return false;
                }
            }
            if (targetId == BeehiveObj)
                return dialog == DialogAction.USE_OBJECT; // loot
            if (targetId == TaturalNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == GilungkNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
