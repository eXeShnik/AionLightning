// Port of Java data/scripts/system/handlers/quest/eltnen/_1347RaidingKlaw.java (Rhys2002).
// Talk to 203965 to start; kill 2 mob groups tracked on separate quest vars (var0 up to 7, var1
// up to 3, no bound gating on the reward flip - faithful to Java, talking to 203966 while START
// immediately flips to REWARD regardless of kill progress); turn in at 203966.
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

namespace Quest.Eltnen;

public sealed class _1347RaidingKlaw : QuestHandlerBase
{
    private const int QuestIdConst = 1347;
    private const int StartNpc     = 203965;
    private const int TurnInNpc    = 203966;

    private static readonly int[] _var0Mobs = [210908, 210874];
    private static readonly int[] _var1Mobs = [212137, 212056, 210917];

    public _1347RaidingKlaw(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        foreach (int mob in _var0Mobs) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
        foreach (int mob in _var1Mobs) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int targetId = env.TargetId;
        if (_var0Mobs.Contains(targetId) && entry.GetVar(0) < 7)
        {
            await ChangeQuestStepAsync(conn, entry, 0, entry.GetVar(0) + 1, toReward: false, ct);
            return true;
        }
        if (_var1Mobs.Contains(targetId) && entry.GetVar(1) < 3)
        {
            await ChangeQuestStepAsync(conn, entry, 1, entry.GetVar(1) + 1, toReward: false, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == StartNpc)
            {
                if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId != TurnInNpc) return false;
            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
            {
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
