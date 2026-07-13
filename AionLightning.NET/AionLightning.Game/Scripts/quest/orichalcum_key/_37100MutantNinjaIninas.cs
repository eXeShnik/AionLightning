// Port of Java data/scripts/system/handlers/quest/orichalcum_key/_37100MutantNinjaIninas.java (Cheatkiller).
// Killing NinjaMob (700967) drops 5x quest item 182210038 at 100% (side-quest drop, any step);
// hand it in at MutantNinjaIninas (799901) via CHECK_USER_HAS_QUEST_ITEM (var 0 -> 1, REWARD).
// Accepted directly (targetId 0, QUEST_ACCEPT_1) - no NPC quest-offer dialog.
// Skip vs Java: talking to 700967 while grouped with an active mentor (player.isInGroup2() +
// member.isMentor() within GroupConfig.GROUP_MAX_DISTANCE) is a pure no-op (Java sends no packet on
// that branch either); the else branch's STR_MSG_DailyQuest_Ask_Mentee notice to non-mentor members
// is skipped too - no mentor/mentee or Group2 model in this port (see
// QuestEngine.Handlers.Templates.MentorMonsterHuntHandler's documented limitation) and this notice
// never changes quest state, so omitting it is harmless.
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

namespace Quest.OrichalcumKey;

public sealed class _37100MutantNinjaIninas : QuestHandlerBase
{
    private const int QuestIdConst = 37100;
    private const int NinjaMobNpc  = 700967;
    private const int TurnInNpc    = 799901;
    private const int DropItem     = 182210038;

    private readonly IItemDao _itemDao;

    public _37100MutantNinjaIninas(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        RegisterQuestDrop(engine, NinjaMobNpc, DropItem, amount: 5, chance: 100);
        engine.RegisterQuestNpc(NinjaMobNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null && targetId == 0 && dialog == DialogAction.QUEST_ACCEPT_1)
        {
            await StartMissionAsync(conn, player, QuestStatus.START, ct);
            return await CloseDialogWindowAsync(conn, 0, ct);
        }

        if (entry is not null && entry.Status == QuestStatus.START)
        {
            // targetId == NinjaMobNpc: mentor-group talk gate skipped - see header.
            if (targetId == TurnInNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 1, true, 5, 2716, ct);
            }
        }
        else if (entry is not null && entry.Status == QuestStatus.REWARD)
        {
            if (targetId == TurnInNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }

        return false;
    }
}
