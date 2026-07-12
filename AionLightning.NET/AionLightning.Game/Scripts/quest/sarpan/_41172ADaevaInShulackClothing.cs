// Port of Java data/scripts/system/handlers/quest/sarpan/_41172ADaevaInShulackClothing.java (Cheatkiller).
// Talk to 205778 to start (no item); talk to disguise object 701160 (var 0->1, reward, no var
// gate in Java — its changeQuestStep call itself re-validates var==0 before applying, this port
// mirrors that internal guard); turn in at 205778.
// Skip vs Java: SkillEngine.applyEffectDirectly(20413, player, player, 0) on QUEST_ACCEPT_SIMPLE
// (a disguise/illusion buff) — no direct-effect-apply service ported (no SkillEngine equivalent
// yet); the quest's state machine is unaffected, only the cosmetic buff is dropped.
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

namespace Quest.Sarpan;

public sealed class _41172ADaevaInShulackClothing : QuestHandlerBase
{
    private const int QuestIdConst = 41172;
    private const int StartNpc      = 205778;
    private const int DisguiseNpc   = 701160;

    public _41172ADaevaInShulackClothing(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(DisguiseNpc).OnTalk.Add(QuestId);
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
            if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == DisguiseNpc)
        {
            if (entry.GetVar(0) == 0)
                await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
            return true;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
