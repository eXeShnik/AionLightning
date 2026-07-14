using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Services;

/// <summary>
/// Inventory ("cube") expansion at an NPC. Mirrors Java <c>services.CubeExpandService.expandCube</c>:
/// looks up the price for the player's next NPC-purchased expand level from cube_expander.xml
/// (<see cref="CubeExpanderData"/>), shows a yes/no confirmation window with the price, then on
/// acceptance charges kinah and grants one more expand level (+9 bag slots per Java CUBE_SPACE).
/// Used by the cube-expand NPC dialog (<see cref="Network.Aion.ClientPackets.CM_DIALOG_SELECT"/>).
/// </summary>
public sealed class CubeExpandService
{
    private const int KinahItemId = 182400001;

    // STR_WAREHOUSE_EXPAND_WARNING — Java SM_QUESTION_WINDOW code for the cube-expand confirmation dialog.
    private const int ExpandWarningQuestionCode = 900686;
    private static readonly TimeSpan ConfirmTimeout = TimeSpan.FromSeconds(30);

    private readonly IPlayerDao _playerDao;
    private readonly IItemDao _itemDao;
    private readonly IDataManager _dataManager;
    private readonly PlayerResponseRegistry _responseRegistry;
    private readonly ILogger<CubeExpandService> _logger;

    public CubeExpandService(IPlayerDao playerDao, IItemDao itemDao, IDataManager dataManager,
        PlayerResponseRegistry responseRegistry, ILogger<CubeExpandService> logger)
    {
        _playerDao        = playerDao;
        _itemDao          = itemDao;
        _dataManager      = dataManager;
        _responseRegistry = responseRegistry;
        _logger           = logger;
    }

    /// <summary>
    /// Validates that <paramref name="npc"/> can expand the player's cube one more level, shows the
    /// price confirmation window, and on acceptance charges kinah and grants the expand. Returns
    /// false (with no state change) whenever validation fails, the player declines, or kinah is short —
    /// never dupes an item and never expands without charging.
    /// </summary>
    public async ValueTask<bool> ExpandCubeAsync(Player player, Npc npc, GsClientConnection conn, CancellationToken ct)
    {
        if (!_dataManager.CubeExpander.IsCubeExpander(npc.Template.NpcId))
        {
            await conn.SendAsync(SM_SYSTEM_MESSAGE.CannotExpandCubeMore(), ct);
            return false;
        }

        int nextLevel = player.NpcExpands + 1;
        long? price = _dataManager.CubeExpander.GetExpandPrice(npc.Template.NpcId, nextLevel);
        if (price is null)
        {
            // Java: outer npcCanExpandLevel/canExpand failure -> raw SM_SYSTEM_MESSAGE(1300430)
            await conn.SendAsync(SM_SYSTEM_MESSAGE.CannotExpandCubeMore(), ct);
            return false;
        }

        if (!await ConfirmAsync(player, price.Value, conn, ct))
            return false;

        var kinah = player.Inventory.FindByItemId(KinahItemId);
        long current = kinah?.Count ?? 0;
        if (current < price.Value)
        {
            await conn.SendAsync(SM_SYSTEM_MESSAGE.CubeExpandNotEnoughMoney(), ct);
            return false;
        }

        kinah!.Count -= price.Value;
        player.NpcExpands++;
        player.Inventory.Capacity = player.CubeCapacity;

        await _playerDao.UpdateCubeExpandAsync(player.ObjectId, player.NpcExpands, ct);
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);

        await conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinah]), ct);
        await conn.SendAsync(SM_CUBE_UPDATE.CubeSize(
            player.Inventory.BagSlotUsed, player.NpcExpands, player.QuestExpands), ct);

        var statTpl = _dataManager.PlayerStats.GetTemplate(player.PlayerClass, player.Level);
        await conn.SendAsync(new SM_STATS_INFO(player, statTpl, _dataManager.ExpTable), ct);
        await conn.SendAsync(SM_SYSTEM_MESSAGE.CubeExpanded(9), ct); // Java: "9 Slots added"

        _logger.LogInformation("Player {Name} expanded cube to {Slots} slots (npcExpands={Level})",
            player.Name, player.CubeCapacity, player.NpcExpands);
        return true;
    }

    // Java: RequestResponseHandler + SM_QUESTION_WINDOW(STR_WAREHOUSE_EXPAND_WARNING, price) round trip.
    private async ValueTask<bool> ConfirmAsync(Player player, long price, GsClientConnection conn, CancellationToken ct)
    {
        var tcs = _responseRegistry.RegisterPending(player.ObjectId);
        try
        {
            await conn.SendAsync(new SM_QUESTION_WINDOW(ExpandWarningQuestionCode, 0, 0, price.ToString()), ct);
        }
        catch
        {
            _responseRegistry.CancelPending(player.ObjectId);
            return false;
        }

        try
        {
            return await tcs.Task.WaitAsync(ConfirmTimeout, ct);
        }
        catch
        {
            _responseRegistry.CancelPending(player.ObjectId);
            return false;
        }
    }
}
