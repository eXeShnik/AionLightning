// Port of Java data/scripts/system/handlers/quest/talocs_hollow/_21468TheStruggleWithin.java (Cheatkiller).
// Elyos counterpart of _11468WithFriendsLikeThese - identical shape and ids (same npcs 799526/799503,
// same skills 9832/9833/9834). Offered at 799526 (OnQuestStart, direct questId dispatch), no items.
// Using skill 9832 counts var 1 up to 10, 9833 counts var 2 up to 5, 9834 counts var 3 up to 3; once
// all three caps are hit the quest flips to REWARD and all three vars reset to 0 (matching Java -
// the counters are not shown again, they only gate the flip). Turn in at 799503.
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

namespace Quest.TalocsHollow;

public sealed class _21468TheStruggleWithin : QuestHandlerBase
{
    private const int QuestIdConst = 21468;
    private const int StartNpc     = 799526;
    private const int TurnInNpc    = 799503;
    private const int Skill1       = 9832;
    private const int Skill2       = 9833;
    private const int Skill3       = 9834;
    private const int Skill1Cap    = 10;
    private const int Skill2Cap    = 5;
    private const int Skill3Cap    = 3;

    public _21468TheStruggleWithin(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterSkillUse(Skill1, QuestId);
        engine.RegisterSkillUse(Skill2, QuestId);
        engine.RegisterSkillUse(Skill3, QuestId);
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if ((entry is null || entry.Status == QuestStatus.NONE) && targetId == StartNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry is { Status: QuestStatus.REWARD } && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnSkillUseAsync(Player player, int skillId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        if (entry is not { Status: QuestStatus.START }) return false;

        if (skillId == Skill1 && entry.GetVar(1) < Skill1Cap)
            entry.SetVar(1, entry.GetVar(1) + 1);
        else if (skillId == Skill2 && entry.GetVar(2) < Skill2Cap)
            entry.SetVar(2, entry.GetVar(2) + 1);
        else if (skillId == Skill3 && entry.GetVar(3) < Skill3Cap)
            entry.SetVar(3, entry.GetVar(3) + 1);
        else
            return false;

        await UpdateQuestStatusAsync(conn, entry, ct);

        if (entry.GetVar(1) == Skill1Cap && entry.GetVar(2) == Skill2Cap && entry.GetVar(3) == Skill3Cap)
        {
            entry.SetVar(1, 0);
            entry.SetVar(2, 0);
            entry.SetVar(3, 0);
            entry.Status = QuestStatus.REWARD;
            await UpdateQuestStatusAsync(conn, entry, ct);
        }

        return false;
    }
}
