// Port of Java data/scripts/system/handlers/quest/theobomos/_3085TheRiddlePoem.java.
// Talk to Serimnir (798144); accepting grants the riddle poem (182208048) if not already held.
// Hand it to the ancient stone (203830, var 0->1, consumes the poem, flips straight to REWARD);
// turn in at Metatron (798132).
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

namespace Quest.Theobomos;

public sealed class _3085TheRiddlePoem : QuestHandlerBase
{
    private const int QuestIdConst = 3085;
    private const int SerimnirNpc  = 798144;
    private const int StoneNpc     = 203830;
    private const int MetatronNpc  = 798132;
    private const int RiddleItemId = 182208048;

    private readonly IItemDao _itemDao;

    public _3085TheRiddlePoem(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(SerimnirNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(SerimnirNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StoneNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MetatronNpc).OnTalk.Add(QuestId);
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
            if (targetId != SerimnirNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_1)
            {
                if ((player.Inventory.FindByItemId(RiddleItemId)?.Count ?? 0) == 0
                    && !await GiveQuestItemAsync(player, conn, _itemDao, RiddleItemId, 1, ct))
                    return true;
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == StoneNpc)
        {
            int var = entry.GetVar(0);
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, RiddleItemId, 1, ct);
                entry.SetVar(0, var + 1);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == MetatronNpc)
        {
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
