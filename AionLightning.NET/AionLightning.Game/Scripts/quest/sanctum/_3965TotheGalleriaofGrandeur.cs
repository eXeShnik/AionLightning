// Port of Java data/scripts/system/handlers/quest/sanctum/_3965TotheGalleriaofGrandeur.java (Rolandas / vlog).
// Talk to Senarinrinerk (798311) to start (gives 2x item 182206120); deliver 1 to Andu (798391,
// var 0->1); deliver the last to Palentine (798390) to finish.
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

namespace Quest.Sanctum;

public sealed class _3965TotheGalleriaofGrandeur : QuestHandlerBase
{
    private const int QuestIdConst = 3965;
    private const int StartNpc     = 798311;
    private const int AnduNpc      = 798391;
    private const int PalentineNpc = 798390;
    private const int TicketItemId = 182206120;

    private readonly IItemDao _itemDao;

    public _3965TotheGalleriaofGrandeur(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AnduNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(PalentineNpc).OnTalk.Add(QuestId);
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
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
                {
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, TicketItemId, 2, ct)) return true;
                    return await SendQuestStartDialogAsync(env, conn, ct);
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == AnduNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 0, 1, reward: false, sameNpc: false,
                        giveItemId: 0, giveItemCount: 0, removeItemId: TicketItemId, removeItemCount: 1, ct);
            }
            else if (targetId == PalentineNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    await ChangeQuestStepAsync(conn, entry, 1, 1, toReward: true, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, TicketItemId, 1, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == PalentineNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
