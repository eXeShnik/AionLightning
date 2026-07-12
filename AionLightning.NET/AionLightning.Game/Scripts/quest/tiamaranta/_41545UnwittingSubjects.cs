// Port of Java data/scripts/system/handlers/quest/tiamaranta/_41545UnwittingSubjects.java
// (Cheatkiller). Talk to 205969 to start (grants item 182212543); casting skill 10380 up to
// three times advances var 0, the third flips to REWARD; turn in at 205969 (removes the item).
// Skip vs Java: onUseSkillEvent also requires the player's current target to be an npc whose name
// starts with "gurriki" - OnSkillUseAsync in this port carries no target information (Java's own
// CM_CASTSPELL wiring only forwards player+skillId, matching the precedent already established by
// sarpan._41304BringOnTheHungryPagatis / greater_stigma._3932StopTheShulacks), so every cast of
// skill 10380 while the quest is active advances the var regardless of target. Doesn't block
// completability; only loosens the original "must cast it on a gurriki" restriction.
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

namespace Quest.Tiamaranta;

public sealed class _41545UnwittingSubjects : QuestHandlerBase
{
    private const int QuestIdConst = 41545;
    private const int StartNpc     = 205969;
    private const int Skill        = 10380;
    private const int StartItemId  = 182212543;

    private readonly IItemDao _itemDao;

    public _41545UnwittingSubjects(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterSkillUse(Skill, QuestId);
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
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
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
                await GiveQuestItemAsync(player, conn, _itemDao, StartItemId, 1, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, StartItemId, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnSkillUseAsync(Player player, int skillId, GsClientConnection conn, CancellationToken ct)
    {
        if (skillId != Skill) return false;

        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var > 2) return false;

        if (var < 2)
            await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
        else
            await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: true, ct);
        return true;
    }
}
