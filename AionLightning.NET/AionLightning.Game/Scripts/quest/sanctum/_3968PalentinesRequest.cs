// Port of Java data/scripts/system/handlers/quest/sanctum/_3968PalentinesRequest.java (Rolandas).
// Talk to Palentine (798390) to start; chain through 798176 (var 0->1, gives 182206123), 204528
// (var 1->2, gives 182206124), 203927 (var 2->3, gives 182206125, reward); return to Palentine to
// finish (removes all three collected items).
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

namespace Quest.Sanctum;

public sealed class _3968PalentinesRequest : QuestHandlerBase
{
    private const int QuestIdConst = 3968;
    private const int StartNpc     = 798390;
    private const int SecondNpc    = 798176;
    private const int ThirdNpc     = 204528;
    private const int FourthNpc    = 203927;
    private const int FirstItemId  = 182206123;
    private const int SecondItemId = 182206124;
    private const int ThirdItemId  = 182206125;

    private readonly IItemDao _itemDao;

    public _3968PalentinesRequest(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(SecondNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ThirdNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FourthNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartNpc)
        {
            if (entry is null || entry.Status == QuestStatus.NONE)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }

        if (entry is null) return false;

        int var = entry.GetVar(0);

        if (targetId == SecondNpc)
        {
            if (entry.Status == QuestStatus.START && var == 0)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    if (await GiveQuestItemAsync(player, conn, _itemDao, FirstItemId, 1, ct))
                    {
                        entry.SetVar(0, var + 1);
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    }
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId == ThirdNpc)
        {
            if (entry.Status == QuestStatus.START && var == 1)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    if (await GiveQuestItemAsync(player, conn, _itemDao, SecondItemId, 1, ct))
                    {
                        entry.SetVar(0, var + 1);
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    }
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId == FourthNpc)
        {
            if (entry.Status == QuestStatus.START && var == 2)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO3)
                {
                    if (await GiveQuestItemAsync(player, conn, _itemDao, ThirdItemId, 1, ct))
                    {
                        entry.SetVar(0, var + 1);
                        entry.Status = QuestStatus.REWARD;
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    }
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT && entry.Status == QuestStatus.REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD && entry.Status is not (QuestStatus.COMPLETE or QuestStatus.NONE))
            {
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                await RemoveQuestItemAsync(player, conn, _itemDao, FirstItemId, 1, ct);
                await RemoveQuestItemAsync(player, conn, _itemDao, SecondItemId, 1, ct);
                await RemoveQuestItemAsync(player, conn, _itemDao, ThirdItemId, 1, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
