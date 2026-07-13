// Port of Java data/scripts/system/handlers/quest/beluslan/_2060RestoringBeluslanObservatory.java.
// Hod (204701, var0->1), Gwendolin (204785, var1->2; var4 checks the collected-aether requirement
// without consuming it, handing out another bottle 182204318 on failure; var4->5 removes the
// gathered aether 182204319), Hisui (278003, var2->3), Glati (278088, var3->4, gives 182204318),
// kill 700290 across [5,8), use-object 700293 at var8 flips to REWARD.
using System.Linq;
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

namespace Quest.Beluslan;

public sealed class _2060RestoringBeluslanObservatory : QuestHandlerBase
{
    private const int QuestIdConst = 2060;
    private const int HodNpc = 204701;
    private const int GwendolinNpc = 204785;
    private const int HisuiNpc = 278003;
    private const int GlatiNpc = 278088;
    private const int DeviceObj = 700293;
    private const int KillNpc = 700290;
    private const int AetherBottleItem = 182204318;
    private const int GatheredAetherItem = 182204319;

    private readonly IItemDao _itemDao;

    public _2060RestoringBeluslanObservatory(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestItem(AetherBottleItem, QuestId);
        engine.RegisterQuestNpc(KillNpc).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(HodNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GwendolinNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HisuiNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GlatiNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(DeviceObj).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 2500, isZoneMission: true, ct);

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, KillNpc, 5, 8, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var = entry.GetVar(0);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == HodNpc)
            {
                if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }
        if (entry.Status != QuestStatus.START) return false;

        if (targetId == HodNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return false;
        }
        if (targetId == GwendolinNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (var == 4)
                {
                    var collectItems = Template?.CollectItems?.Items;
                    bool hasAll = collectItems is { Count: > 0 } && collectItems.All(req =>
                    {
                        var item = player.Inventory.FindByItemId(req.ItemId);
                        return item is not null && item.Count >= req.Count;
                    });
                    if (hasAll) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    await GiveQuestItemAsync(player, conn, _itemDao, AetherBottleItem, 1, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 2461, ct);
                }
                return false;
            }
            if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            if (dialog == DialogAction.SETPRO5)
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 4, 5, reward: false, sameNpc: false,
                    giveItemId: 0, giveItemCount: 0, removeItemId: GatheredAetherItem, removeItemCount: 1, ct);
            if (dialog == DialogAction.FINISH_DIALOG && var == 4) return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            return false;
        }
        if (targetId == HisuiNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SETPRO3) return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
            return false;
        }
        if (targetId == GlatiNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            if (dialog == DialogAction.SETPRO4)
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 3, 4, reward: false, sameNpc: false,
                    giveItemId: AetherBottleItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
            return false;
        }
        if (targetId == DeviceObj)
        {
            if (dialog == DialogAction.USE_OBJECT && var == 8)
                return await UseQuestObjectAsync(env, conn, 8, 8, true, 0, ct);
            return false;
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != AetherBottleItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 4) return false;
        if (!player.CurrentZones.Contains("AB1_ITEMUSEAREA_Q2060")) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, AetherBottleItem, 1, ct);
        return await GiveQuestItemAsync(player, conn, _itemDao, GatheredAetherItem, 1, ct);
    }
}
