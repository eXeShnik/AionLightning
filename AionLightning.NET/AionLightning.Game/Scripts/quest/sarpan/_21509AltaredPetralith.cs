// Port of Java data/scripts/system/handlers/quest/sarpan/_21509AltaredPetralith.java (zhkchi).
// Talk to 205782 to start (no item); entering WARRIORS_SHRINE_600020000 while var 0 flips straight
// to REWARD; turn in at 205782.
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

public sealed class _21509AltaredPetralith : QuestHandlerBase
{
    private const int QuestIdConst = 21509;
    private const int StartNpc     = 205782;
    private const string ShrineZone = "WARRIORS_SHRINE_600020000";

    public _21509AltaredPetralith(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, ShrineZone);
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
            if (dialog == DialogAction.QUEST_REFUSE_SIMPLE) return await CloseDialogWindowAsync(conn, targetObjId, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != ShrineZone) return false;

        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.GetVar(0) != 0) return false;

        entry.Status = QuestStatus.REWARD;
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }
}
