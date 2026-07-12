// Port of Java data/scripts/system/handlers/quest/pandaemonium/_29048SeriphimTeachings.java.
// Accept at 798304 gives quest item 182212217 x1; using that item while START and var 0 == 0
// sets var0=1 then var1=1 + REWARD (Java's own two-step write, ported as-is — harmless, nothing
// else reads var slot 1); turn in at 798304 removes the item first.
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

namespace Quest.Pandaemonium;

public sealed class _29048SeriphimTeachings : QuestHandlerBase
{
    private const int QuestIdConst = 29048;
    private const int Npc = 798304;
    private const int QuestItem = 182212217;

    private readonly IItemDao _itemDao;

    public _29048SeriphimTeachings(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(Npc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Npc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(QuestItem, QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != QuestItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 0) return false;

        entry.SetVar(0, 1);
        await ChangeQuestStepAsync(conn, entry, 1, 1, toReward: true, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != Npc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (!await GiveQuestItemAsync(player, conn, _itemDao, QuestItem, 1, ct)) return false;
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD && targetId == Npc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, QuestItem, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
