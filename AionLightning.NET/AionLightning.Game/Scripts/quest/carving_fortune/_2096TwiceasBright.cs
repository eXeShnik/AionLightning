// Port of Java data/scripts/system/handlers/quest/carving_fortune/_2096TwiceasBright.java
// (Manu72, reworked vlog). Talk to Cavalorn (204206, var 0->1), Kasir (204207, var 1->2), then
// Munin (203550, var 2 -> REWARD). Level-up gated on any of 2007/2022/2041/2094/2061/2076/2900.
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

namespace Quest.CarvingFortune;

public sealed class _2096TwiceasBright : QuestHandlerBase
{
    private const int QuestIdConst = 2096;
    private const int CavalornNpc  = 204206;
    private const int KasirNpc     = 204207;
    private const int MuninNpc     = 203550;

    private static readonly int[] _precedingQuestIds = [2007, 2022, 2041, 2094, 2061, 2076, 2900];

    public _2096TwiceasBright(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(CavalornNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KasirNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MuninNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, _precedingQuestIds, isZoneMission: false, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);
        int var         = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            switch (targetId)
            {
                case CavalornNpc:
                    if (dialog == DialogAction.QUEST_SELECT && var == 0)
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (dialog == DialogAction.SETPRO1)
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                    return false;

                case KasirNpc:
                    if (dialog == DialogAction.QUEST_SELECT && var == 1)
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (dialog == DialogAction.SETPRO2)
                        return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                    return false;

                case MuninNpc:
                    if (dialog == DialogAction.QUEST_SELECT && var == 2)
                    {
                        await ChangeQuestStepAsync(conn, entry, 2, 2, toReward: true, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    }
                    return false;

                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == MuninNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
