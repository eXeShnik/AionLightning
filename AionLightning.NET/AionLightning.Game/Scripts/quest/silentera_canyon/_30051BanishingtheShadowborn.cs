// Port of Java data/scripts/system/handlers/quest/silentera_canyon/_30051BanishingtheShadowborn.java (zhkchi).
// [Daily] Elyos PvP-kill quest: talk to Vira (799381) to start, kill 7 opposing-race players
// anywhere in Silentera Canyon (world 600010000) within a level bracket, turn in at the same npc.
// Skip vs Java: qs.canRepeat() (daily-repeat) isn't ported - approximated as "no active entry"
// like the rest of this port (see _1710Defeat1thRankAsmodianSoldiers in reshanta), so this
// completes once per character instead of resetting daily.
// Java bug: the level-bracket check used || between (killed.Level+9 >= player.Level) and
// (killed.Level-5 <= player.Level), which is a tautology (always true) - every kill counted
// regardless of level difference. Changed to && for a real bracket: player.Level must sit within
// [killed.Level-9, killed.Level+5].
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

namespace Quest.SilenteraCanyon;

public sealed class _30051BanishingtheShadowborn : QuestHandlerBase
{
    private const int QuestIdConst      = 30051;
    private const int StartNpc          = 799381;
    private const int SilenteraWorldId  = 600010000;

    public _30051BanishingtheShadowborn(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterKillInWorld(SilenteraWorldId, QuestId);
    }

    public override async ValueTask<bool> OnPlayerKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.Target is not Player killed) return false;
        if (!(killed.Level + 9 >= env.Player.Level && killed.Level - 5 <= env.Player.Level)) return false;

        int var = entry.GetVar(0);
        if (var < 6)
        {
            await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
            return true;
        }
        if (var == 6)
        {
            await ChangeQuestStepAsync(conn, entry, -1, 0, toReward: true, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        if (env.TargetId != StartNpc) return false;

        var entry       = env.Player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
