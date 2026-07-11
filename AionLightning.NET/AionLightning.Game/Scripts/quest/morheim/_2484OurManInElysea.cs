// Port of Java data/scripts/system/handlers/quest/morheim/_2484OurManInElysea.java.
// Start at 204407 (grants the letter 182204205 on accept); using the parchment object (700267,
// var 0->1) consumes the letter; turning in at either the parchment or Rurio (203331) shows the
// tier-5 confirm page and, on QUEST_SELECT, flips to REWARD; final turn-in is at Rurio.
// Java's onDialogEvent has a switch case 700267 that falls through (no break) into case 203331's
// var==1 check on every call, so both targets share identical var==1 handling once the letter is
// consumed — ported directly as an equivalent (non-fallthrough) if/else below rather than
// replicating the switch fallthrough literally.
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

namespace Quest.Morheim;

public sealed class _2484OurManInElysea : QuestHandlerBase
{
    private const int QuestIdConst  = 2484;
    private const int StartNpc      = 204407;
    private const int ParchmentObj  = 700267;
    private const int ReceiverNpc   = 203331;
    private const int LetterItemId  = 182204205;

    private readonly IItemDao _itemDao;

    public _2484OurManInElysea(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ParchmentObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ReceiverNpc).OnTalk.Add(QuestId);
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
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                if (!await GiveQuestItemAsync(player, conn, _itemDao, LetterItemId, 1, ct)) return true;
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == ParchmentObj && entry.GetVar(0) == 0 && dialog == DialogAction.USE_OBJECT)
            {
                entry.SetVar(0, 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await RemoveQuestItemAsync(player, conn, _itemDao, LetterItemId, 1, ct);
                return false;
            }

            if ((targetId == ParchmentObj || targetId == ReceiverNpc) && entry.GetVar(0) == 1)
            {
                if (env.DialogId == (int)DialogAction.SELECTED_QUEST_NOREWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == ReceiverNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
