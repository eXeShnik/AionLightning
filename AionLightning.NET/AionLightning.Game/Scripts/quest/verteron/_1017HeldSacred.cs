// Port of Java data/scripts/system/handlers/quest/verteron/_1017HeldSacred.java (MrPoke, Dune11,
// Rhys2002). Talk to Krotan (203178), advance var 0->1, then collect-check turns in (var ->2,
// REWARD). Zone-mission-end/level-up gated on quest 1130.
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

public sealed class _1017HeldSacred : QuestHandlerBase
{
    private const int QuestIdConst = 1017;
    private const int KrotanNpc    = 203178;

    private readonly IItemDao _itemDao;

    public _1017HeldSacred(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(KrotanNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1130, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var = entry.GetVar(0);
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START && env.TargetId == KrotanNpc)
        {
            switch (DialogActionLookup.FromId(env.DialogId))
            {
                case DialogAction.QUEST_SELECT when var == 0:
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                case DialogAction.QUEST_SELECT when var == 1:
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                case DialogAction.SETPRO1 when var == 0:
                    entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                case DialogAction.CHECK_USER_HAS_QUEST_ITEM when var == 1:
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 2, true, 5, 1353, ct);
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && env.TargetId == KrotanNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
