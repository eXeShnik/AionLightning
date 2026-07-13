// Port of Java data/scripts/system/handlers/quest/beshmundir/_30239NewSpellbook.java (vlog).
// Requires owning the Noble Siel's Supreme Spellbook (100600787) to start at Gefeios (799032); kill
// any of the three Debilkarim (286904/281419/215795) while all four quest_data.xml collect_items
// (base weapon + essences 186000099/186000106/186000107) are held to consume them and grant the
// quest-work token (182209637), flipping straight to REWARD; turn in at Gefeios.
// Java bug/skip: same GiveQuestItemAsync/OnItemGetAsync non-dispatch as _30235NewSword - see that
// file's header for the established precedent (eltnen/_1037SecretsoftheTemple).
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

namespace Quest.Beshmundir;

public sealed class _30239NewSpellbook : QuestHandlerBase
{
    private const int QuestIdConst = 30239;
    private const int StartNpc     = 799032; // Gefeios
    private const int BaseWeaponItemId = 100600787; // Noble Siel's Supreme Spellbook
    private const int QuestWorkItemId  = 182209637;
    private static readonly int[] DebilkarimNpcIds = [286904, 281419, 215795];

    private readonly IItemDao _itemDao;

    public _30239NewSpellbook(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterItemGet(QuestWorkItemId, QuestId);
        foreach (int npcId in DebilkarimNpcIds)
            engine.RegisterQuestNpc(npcId).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (env.TargetId != StartNpc) return false;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (player.Inventory.FindByItemId(BaseWeaponItemId) is not { Count: >= 1 }) return false;

            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (dialog == DialogAction.USE_OBJECT)
            {
                var item = player.Inventory.FindByItemId(QuestWorkItemId);
                if (item is not null && item.Count > 0) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return false;
            }

            await RemoveQuestItemAsync(player, conn, _itemDao, QuestWorkItemId, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (!DebilkarimNpcIds.Contains(env.TargetId)) return false;

        if (!await TryConsumeCollectItemsAsync(player, conn, ct)) return false;
        if (!await GiveQuestItemAsync(player, conn, _itemDao, QuestWorkItemId, 1, ct)) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
        return true;
    }

    public override ValueTask<bool> OnItemGetAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
        => ValueTask.FromResult(false); // see header - never fires via GiveQuestItemAsync; registered for parity only.

    private async ValueTask<bool> TryConsumeCollectItemsAsync(Player player, GsClientConnection conn, CancellationToken ct)
    {
        var collectItems = Template?.CollectItems?.Items;
        if (collectItems is not { Count: > 0 }) return false;

        foreach (var req in collectItems)
        {
            var item = player.Inventory.FindByItemId(req.ItemId);
            if (item is null || item.Count < req.Count) return false;
        }

        foreach (var req in collectItems)
            await RemoveQuestItemAsync(player, conn, _itemDao, req.ItemId, req.Count, ct);

        return true;
    }
}
