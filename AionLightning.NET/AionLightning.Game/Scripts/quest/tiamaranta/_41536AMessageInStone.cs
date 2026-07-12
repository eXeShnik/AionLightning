// Port of Java data/scripts/system/handlers/quest/tiamaranta/_41536AMessageInStone.java (Cheatkiller).
// Talk to 205944 to start; interact with 3 stones in sequence (701238/701239/701240), each
// granting a rubbing item and advancing the var; return all 3 rubbings to 205944 to turn in.
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

namespace Quest.Tiamaranta;

public sealed class _41536AMessageInStone : QuestHandlerBase
{
    private const int QuestIdConst = 41536;
    private const int StartNpc     = 205944;
    private const int Stone1Obj    = 701238;
    private const int Stone2Obj    = 701239;
    private const int Stone3Obj    = 701240;
    private const int Rubbing1ItemId = 182212534;
    private const int Rubbing2ItemId = 182212535;
    private const int Rubbing3ItemId = 182212536;

    private readonly IItemDao _itemDao;

    public _41536AMessageInStone(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Stone1Obj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Stone2Obj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Stone3Obj).OnTalk.Add(QuestId);
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
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId == Stone1Obj && var == 0)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, Rubbing1ItemId, 1, ct);
                    await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return false;
            }

            if (targetId == Stone2Obj && var == 1)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, Rubbing2ItemId, 1, ct);
                    await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return false;
            }

            if (targetId == Stone3Obj && var == 2)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO3)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, Rubbing3ItemId, 1, ct);
                    await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return false;
            }

            if (targetId == StartNpc && var == 3)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, Rubbing1ItemId, 1, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, Rubbing2ItemId, 1, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, Rubbing3ItemId, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 3, 3, reward: true, sameNpc: true, ct);
                }
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
