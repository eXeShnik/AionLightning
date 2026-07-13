// Port of Java data/scripts/system/handlers/quest/kaisinel_academy/_37000ToxicInstruction.java
// (Cheatkiller). Sidequest item drop from 700967 (182210034 x5 @100%, any step); collect 1 at
// 799835 via CHECK_USER_HAS_QUEST_ITEM to flip to REWARD, then turn in. Elyos-side mirror of
// marchutan_priory's _47000AltgardOrbIt (same mentor-gap reasoning for 700967 — see that file's
// header).
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

namespace Quest.KaisinelAcademy;

public sealed class _37000ToxicInstruction : QuestHandlerBase
{
    private const int QuestIdConst = 37000;
    private const int MentorNpc    = 700967; // group2/mentor-gated NPC — no reachable dialog case (see marchutan_priory/_47000AltgardOrbIt)
    private const int TurnInNpc    = 799835;
    private const int DropItemId   = 182210034;

    private readonly IItemDao _itemDao;

    public _37000ToxicInstruction(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        RegisterQuestDrop(engine, MentorNpc, DropItemId, 5, 100);
        engine.RegisterQuestNpc(MentorNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId == 0 && dialog == DialogAction.QUEST_ACCEPT_1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await CloseDialogWindowAsync(conn, 0, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == TurnInNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 1, reward: true, checkOkId: 5, checkFailId: 2716, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
