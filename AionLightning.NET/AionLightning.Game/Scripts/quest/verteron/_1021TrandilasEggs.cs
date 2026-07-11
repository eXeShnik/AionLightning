// Port of Java data/scripts/system/handlers/quest/verteron/_1021TrandilasEggs.java (MrPoke).
// Talk to Baaruk (203129), advance var 0->1, kill 210202 once (var 1 -> REWARD).
// Zone-mission-end gated on quest 1015; level-up gated on quests 1130 and 1015.
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Verteron;

public sealed class _1021TrandilasEggs : QuestHandlerBase
{
    private const int QuestIdConst = 1021;
    private const int BaarukNpc    = 203129;
    private const int MobNpc       = 210202;

    public _1021TrandilasEggs(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(BaarukNpc).OnTalk.Add(QuestId);
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(MobNpc).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, precedingQuestId: 1015, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, (IReadOnlyCollection<int>)[1130, 1015], isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || env.TargetId != BaarukNpc) return false;

        int var = entry.GetVar(0);
        int targetObjId = env.Target?.ObjectId ?? 0;

        switch (DialogActionLookup.FromId(env.DialogId))
        {
            case DialogAction.QUEST_SELECT when var == 0:
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            case DialogAction.SETPRO1 or DialogAction.SETPRO2 when var == 0:
                entry.SetVar(0, var + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            default:
                return false;
        }
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || env.TargetId != MobNpc) return false;
        if (entry.GetVar(0) != 1) return false;

        entry.Status = QuestStatus.REWARD;
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }
}
