// Port of Java data/scripts/system/handlers/quest/fort_tiamat/_20071TheHuntForIsraphel.java (Cheatkiller).
// Asmodian mirror of _10071OnTheTrailOfIsraphel: 205579 (var0->1) -> 205617 (var1->2, gives item
// 182213249) -> 205987 (var2->3, removes 182213249; var4->5; var5->6 reward, gives item 182213250)
// -> Mysterious Orb 730465 (USE_OBJECT, var3->4). Turn in back at 205617.
// Java bug fixed: register() lists npc 295987 (typo) instead of the 205987 actually checked in
// onDialogEvent, so the hub npc's OnTalk hook was never wired up in the original; registered the
// correct id here (and dropped the accidental duplicate 730465 entry).
// Caveat: onLvlUpEvent gates this quest behind quest 20070 (_20070MarchutanSWill), skipped in this
// port (needs InstanceService.getNextAvailableInstance, unavailable) — this handler is fully
// functional but currently unreachable via the level-up path.
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

namespace Quest.FortTiamat;

public sealed class _20071TheHuntForIsraphel : QuestHandlerBase
{
    private const int QuestIdConst = 20071;
    private const int StartNpc     = 205579;
    private const int ItemNpc      = 205617;
    private const int HubNpc       = 205987;
    private const int OrbNpc       = 730465;
    private const int TokenItem    = 182213249;
    private const int RewardItem   = 182213250;

    private readonly IItemDao _itemDao;

    public _20071TheHuntForIsraphel(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ItemNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HubNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(OrbNpc).OnTalk.Add(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 20070, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != ItemNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);

        if (targetId == StartNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 0:
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                case DialogAction.SETPRO1 when var == 0:
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                default:
                    return false;
            }
        }

        if (targetId == ItemNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 1:
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                case DialogAction.SETPRO2 when var == 1:
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, TokenItem, 1, ct)) return false;
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                default:
                    return false;
            }
        }

        if (targetId == HubNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 2:
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                case DialogAction.QUEST_SELECT when var == 4:
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                case DialogAction.QUEST_SELECT when var == 5:
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                case DialogAction.SETPRO3 when var == 2:
                    await RemoveQuestItemAsync(player, conn, _itemDao, TokenItem, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                case DialogAction.SETPRO5 when var == 4:
                    return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                case DialogAction.SET_SUCCEED when var == 5:
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 5, 6, reward: true, sameNpc: false,
                        giveItemId: RewardItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                default:
                    return false;
            }
        }

        if (targetId == OrbNpc && dialog == DialogAction.USE_OBJECT && var == 3)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: false, ct);
            return true;
        }

        return false;
    }
}
