// Port of Java data/scripts/system/handlers/quest/fort_tiamat/_30713JustATest.java (Cheatkiller).
// Accept at 205892 (page 1011, gives item 182213264 before starting — Java's
// giveQuestItem/sendQuestStartDialog(QUEST_ACCEPT_SIMPLE) pair); 730701 (var0->1, gives item
// 182213265 and removes 182213264); 205987 (SELECT_QUEST_REWARD removes 182213265 and flips
// straight to REWARD, same npc shows the turn-in page immediately). Turn in at 205987.
// Java bug worked around: the accept action is QUEST_ACCEPT_SIMPLE, which the ported
// QuestHandlerBase.SendQuestStartDialogAsync doesn't handle (it only recognizes QUEST_ACCEPT/
// QUEST_ACCEPT_1/QUEST_REFUSE*); ported directly via StartMissionAsync + CloseDialogWindowAsync,
// matching Java's own sendQuestStartDialog(QUEST_ACCEPT_SIMPLE) path.
// Ported as-is (not a guarded give): Java calls giveQuestItem for 182213264/182213265 without
// checking the return value, so a full inventory silently drops the item instead of blocking the
// step transition.
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

namespace Quest.FortTiamat;

public sealed class _30713JustATest : QuestHandlerBase
{
    private const int QuestIdConst = 30713;
    private const int StartNpc     = 205892;
    private const int MidNpc       = 730701;
    private const int TurnInNpc    = 205987;
    private const int TokenItem    = 182213264;
    private const int ProofItem    = 182213265;

    private readonly IItemDao _itemDao;

    public _30713JustATest(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MidNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
            {
                await GiveQuestItemAsync(player, conn, _itemDao, TokenItem, 1, ct);
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == MidNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, ProofItem, 1, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, TokenItem, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                return false;
            }
            if (targetId == TurnInNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, ProofItem, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 1, 1, reward: true, sameNpc: true, ct);
                }
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
