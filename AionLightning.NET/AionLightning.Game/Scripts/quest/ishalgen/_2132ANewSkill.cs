// Port of Java data/scripts/system/handlers/quest/ishalgen/_2132ANewSkill.java.
// Asmodian mirror of Poeta 1205: level-up auto-start → REWARD with a class-keyed var; turn in
// at the trainer NPC for your starting class.
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

namespace Quest.Ishalgen;

public sealed class _2132ANewSkill : QuestHandlerBase
{
    private const int QuestIdConst = 2132;

    private static readonly System.Collections.Generic.Dictionary<int, (PlayerClass Cls, int IntroPage, int RewardPage)> _trainers = new()
    {
        [203527] = (PlayerClass.WARRIOR,  1011, 5),
        [203528] = (PlayerClass.SCOUT,    1352, 6),
        [203529] = (PlayerClass.MAGE,     1693, 7),
        [203530] = (PlayerClass.PRIEST,   2034, 8),
        [801218] = (PlayerClass.ENGINEER, 2375, 45),
        [801219] = (PlayerClass.ARTIST,   2716, 46),
    };

    public _2132ANewSkill(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npcId in _trainers.Keys)
            engine.RegisterQuestNpc(npcId).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        if (Template is null || player.Level < Template.MinLevel) return false;
        if (player.Quests.Get(QuestId) is not null) return false;

        if (!await StartMissionAsync(conn, player, QuestStatus.REWARD, ct)) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var = player.PlayerClass.GetStartingClassFor() switch
        {
            PlayerClass.WARRIOR  => 1,
            PlayerClass.SCOUT    => 2,
            PlayerClass.MAGE     => 3,
            PlayerClass.PRIEST   => 4,
            PlayerClass.ENGINEER => 5,
            PlayerClass.ARTIST   => 6,
            _                    => 0,
        };
        entry.SetVar(0, var);
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.REWARD) return false;
        if (!_trainers.TryGetValue(env.TargetId, out var t)) return false;
        if (player.PlayerClass.GetStartingClassFor() != t.Cls) return false;

        int targetObjId = env.Target?.ObjectId ?? 0;
        if (DialogActionLookup.FromId(env.DialogId) == DialogAction.USE_OBJECT)
            return await SendQuestDialogAsync(conn, targetObjId, t.IntroPage, ct);
        if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
            return await SendQuestDialogAsync(conn, targetObjId, t.RewardPage, ct);
        return await SendQuestEndDialogAsync(env, conn, ct);
    }
}
