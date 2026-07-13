// Port of Java data/scripts/system/handlers/quest/beshmundir/_30213GroupMagicalEssence.java (Gigi).
// Talk to 798941 to start; at 730275, SETPRO1 removes item 182209617 and flips straight to REWARD;
// turn in at 798926 (a different npc than the start npc - USE_OBJECT shows a preview page,
// SELECT_QUEST_REWARD shows the reward list, any other dialog falls through to the normal
// end-dialog flow).
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

namespace Quest.Beshmundir;

public sealed class _30213GroupMagicalEssence : QuestHandlerBase
{
    private const int QuestIdConst = 30213;
    private const int StartNpc     = 798941;
    private const int SetPro1Npc   = 730275;
    private const int TurnInNpc    = 798926;
    private const int EssenceItem  = 182209617;

    private readonly IItemDao _itemDao;

    public _30213GroupMagicalEssence(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SetPro1Npc).OnTalk.Add(QuestId);
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
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == SetPro1Npc)
        {
            if (dialog == DialogAction.SETPRO1)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, EssenceItem, 1, ct);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            return dialog switch
            {
                DialogAction.USE_OBJECT          => await SendQuestDialogAsync(conn, targetObjId, 10002, ct),
                DialogAction.SELECT_QUEST_REWARD => await SendQuestDialogAsync(conn, targetObjId, 5, ct),
                _ => await SendQuestEndDialogAsync(env, conn, ct),
            };
        }

        return false;
    }
}
