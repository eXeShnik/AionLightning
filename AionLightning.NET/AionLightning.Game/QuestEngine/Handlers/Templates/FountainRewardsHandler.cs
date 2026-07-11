using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Model.Templates.Quest;
using AionLightning.Game.Model.Templates.Quest.Script;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace AionLightning.Game.QuestEngine.Handlers.Templates;

/// <summary>
/// Data-driven "coin fountain" exchange handler (Java
/// <c>questEngine.handlers.template.FountainRewards</c> port) — covers &lt;fountain_rewards&gt;
/// entries (7 on disk). Each entry's quest_data.xml template gates on a coin item declared via
/// &lt;inventory_items&gt; (distinct from &lt;collect_items&gt; — see
/// <see cref="Model.Templates.Quest.InventoryItemsHolder"/>, added in this phase) and grants a
/// fixed reward (usually exp) on turn-in.
/// </summary>
/// <remarks>
/// Java's real flow is a single "insert coin" click (<c>USE_OBJECT</c> then <c>SETPRO1</c>) that
/// starts the quest AND transitions it straight to REWARD in one action — it never meaningfully
/// occupies START status. Neither of those Java dialog ids is routed to the quest engine by
/// <c>CM_DIALOG_SELECT</c> (only <c>QUEST_SELECT</c>/<c>QUEST_ACCEPT(_1)</c>/
/// <c>SELECT_QUEST_REWARD</c> are), so this port re-creates the same "create already in REWARD"
/// behavior off the routed <c>QUEST_ACCEPT</c>/<c>QUEST_ACCEPT_1</c> click instead (see
/// <see cref="TryStartAndAdvanceAsync"/>) — a documented adaptation, not a literal dialog-id port.
/// Not ported: <c>isFullSpecialCube()</c> (a housing-cube-extension inventory check) and
/// quest-repeat (<c>max_repeat_count="255"</c>) — the latter is the same pre-existing gap already
/// noted for every template handler in this engine (see <c>QuestEngine.ComputeNearbyQuests</c> and
/// <c>WorkOrdersHandler</c> remarks).
/// </remarks>
public sealed class FountainRewardsHandler : QuestHandlerBase
{
    private readonly HashSet<int> _startNpcs;

    public FountainRewardsHandler(FountainRewardsScriptEntry data, IDataManager dataManager,
        IQuestDao questDao, QuestRewardService rewardService)
        : base(data.Id, dataManager, questDao, rewardService)
    {
        _startNpcs = data.StartNpcIds;
    }

    public override void Register(QuestEngine engine)
    {
        foreach (int npcId in _startNpcs)
        {
            var npc = engine.RegisterQuestNpc(npcId);
            npc.OnQuestStart.Add(QuestId);
            npc.OnTalk.Add(QuestId);
        }
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var template = Template;
        if (template is null) return false;

        var player      = env.Player;
        int targetId     = env.TargetId;
        int targetObjId  = env.Target?.ObjectId ?? 0;
        if (!_startNpcs.Contains(targetId)) return false;

        var entry  = player.Quests.Get(QuestId);
        var status = entry?.Status ?? QuestStatus.NONE;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        switch (status)
        {
            case QuestStatus.NONE:
                if (player.Level < template.MinLevel) return false;

                return dialog switch
                {
                    DialogAction.QUEST_SELECT => await SendQuestDialogAsync(conn, targetObjId,
                        HasRequiredCoin(player, template) ? 1011 : 1009, ct),
                    DialogAction.QUEST_ACCEPT or DialogAction.QUEST_ACCEPT_1
                        => await TryStartAndAdvanceAsync(env, conn, template, ct),
                    DialogAction.QUEST_REFUSE or DialogAction.QUEST_REFUSE_1
                        or DialogAction.QUEST_REFUSE_2 or DialogAction.QUEST_REFUSE_SIMPLE
                        => await CloseDialogWindowAsync(conn, targetObjId, ct),
                    _ => false,
                };

            case QuestStatus.REWARD:
                return dialog switch
                {
                    DialogAction.QUEST_SELECT        => await SendQuestDialogAsync(conn, targetObjId, 1352, ct),
                    DialogAction.SELECT_QUEST_REWARD => await SendQuestEndDialogAsync(env, conn, ct),
                    _ => false,
                };

            default:
                return false;
        }
    }

    private static bool HasRequiredCoin(Player player, QuestTemplate template)
    {
        var items = template.InventoryItems?.Items;
        if (items is not { Count: > 0 }) return true;
        foreach (var req in items)
            if ((player.Inventory.FindByItemId(req.ItemId)?.Count ?? 0) < req.Count) return false;
        return true;
    }

    /// <summary>
    /// Creates the quest entry directly in REWARD status (Java's <c>startQuest</c> +
    /// <c>changeQuestStep(..., toReward: true)</c> combo, fired together off a single "insert coin"
    /// click — see remarks above). Requires the coin item up front, mirroring Java's
    /// <c>inventoryItemCheck</c> exploit guard.
    /// </summary>
    private async ValueTask<bool> TryStartAndAdvanceAsync(QuestEnv env, GsClientConnection conn, QuestTemplate template, CancellationToken ct)
    {
        var player      = env.Player;
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (player.Quests.Contains(QuestId)) return false;
        if (!HasRequiredCoin(player, template)) return false;

        var entry = new QuestEntry { QuestId = QuestId, Status = QuestStatus.REWARD };
        player.Quests.Add(entry);
        await QuestDao.UpsertAsync(player.ObjectId, entry, ct);

        await conn.SendAsync(new SM_QUEST_ACTION(entry.QuestId,
            SM_QUEST_ACTION.ActionType.Accept, (byte)entry.Status, entry.Step), ct);
        await conn.SendAsync(new SM_QUEST_LIST(player.Quests.Active), ct);

        return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
    }
}
