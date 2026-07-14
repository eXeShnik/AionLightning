using AionLightning.Game.Configs.Options;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.House;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Model.Templates.Housing;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Services;

/// <summary>
/// P1 housing subsystem (Java services.HousingService — only the load-at-startup + ownership maps +
/// getters + login owner-flags/broadcast subset is ported here). Bidding, maintenance/fee scheduling,
/// spawning house objects/NPCs into the world and the studio-purchase flow are P2+ and not present.
/// The SM_HOUSE_OWNER_INFO client broadcast is gated behind <see cref="HousingOptions.Enable"/>
/// (default false) — see HousingOptions for the rationale.
/// </summary>
public sealed class HousingService(
    IHouseDao houseDao,
    IDataManager dataManager,
    IOptions<HousingOptions> options,
    ILogger<HousingService> log)
{
    // Keyed by HouseAddress.Id (Java customHouses).
    private readonly Dictionary<int, House> _customHouses = new();
    // Keyed by owning player's objectId (Java studios).
    private readonly Dictionary<int, House> _studios = new();
    private readonly object _lock = new();

    /// <summary>Java HousingService()'s constructor-time DAOManager.getDAO(HousesDAO.class).loadHouses
    /// calls — loads every persisted house row and splits it into the studio/custom-house maps by
    /// building type (PERSONAL_INS = studio, keyed by owner; everything else = custom house, keyed by
    /// address). Called once at startup by HousingServiceHostedService.</summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        var houses = await houseDao.LoadAllAsync(ct);

        lock (_lock)
        {
            _customHouses.Clear();
            _studios.Clear();
            foreach (var house in houses)
            {
                if (IsStudioBuilding(house.BuildingId))
                    _studios[house.PlayerObjectId] = house;
                else
                    _customHouses[house.Address] = house;
            }
        }

        log.LogInformation("HousingService: loaded {Custom} custom house(s), {Studios} studio(s)",
            _customHouses.Count, _studios.Count);
    }

    private bool IsStudioBuilding(int buildingId) =>
        dataManager.Housing.GetBuilding(buildingId)?.Type == BuildingType.PERSONAL_INS;

    /// <summary>Java searchPlayerHouses(int).</summary>
    public List<House> SearchPlayerHouses(int playerObjId)
    {
        lock (_lock)
        {
            if (_studios.TryGetValue(playerObjId, out var studio))
                return [studio];

            return _customHouses.Values.Where(h => h.PlayerObjectId == playerObjId).ToList();
        }
    }

    /// <summary>Java getPlayerAddress(int).</summary>
    public int GetPlayerAddress(int playerId)
    {
        lock (_lock)
        {
            if (_studios.TryGetValue(playerId, out var studio))
                return studio.Address;

            foreach (var house in _customHouses.Values)
            {
                if (house.Status == HouseStatus.Inactive)
                    continue;
                if (house.PlayerObjectId == playerId && house.Status is HouseStatus.Active or HouseStatus.SellWait)
                    return house.Address;
            }
        }
        return 0;
    }

    /// <summary>Java getHouseByAddress(int).</summary>
    public House? GetHouseByAddress(int address)
    {
        lock (_lock)
        {
            return _customHouses.Values.FirstOrDefault(h => h.Address == address);
        }
    }

    /// <summary>Java getCustomHouses() — a snapshot of every custom house tracked by this service,
    /// regardless of ownership. Used by HousingBidService to correlate persisted bids/auto-fill auctions
    /// to houses at startup.</summary>
    public List<House> GetCustomHouses()
    {
        lock (_lock)
        {
            return _customHouses.Values.ToList();
        }
    }

    /// <summary>Java getPlayerStudio(int).</summary>
    public House? GetPlayerStudio(int playerId)
    {
        lock (_lock)
        {
            return _studios.GetValueOrDefault(playerId);
        }
    }

    /// <summary>Java onPlayerLogin(Player) — owner-flags computation always runs; only the
    /// SM_HOUSE_OWNER_INFO broadcast is gated behind <see cref="HousingOptions.Enable"/>. The Java
    /// method's trailing SM_FRIEND_LIST/SM_MARK_FRIENDLIST sends are handled elsewhere in this port's
    /// enter-world flow, not here.</summary>
    public async ValueTask OnPlayerLoginAsync(Player player, GsClientConnection conn, CancellationToken ct = default)
    {
        player.Houses.Clear();
        player.Houses.AddRange(SearchPlayerHouses(player.ObjectId));

        var activeHouse = player.ActiveHouse;
        byte buildingState = (byte)PlayerHouseOwnerFlags.BuyStudioAllowed;
        if (activeHouse is null)
        {
            int questId = player.Race == Race.ELYOS ? 18802 : 28802;
            if (player.Quests.Get(questId)?.Status == QuestStatus.COMPLETE)
                buildingState |= (byte)PlayerHouseOwnerFlags.BiddingAllowed;
        }
        else
        {
            buildingState = activeHouse.Status == HouseStatus.SellWait
                ? (byte)PlayerHouseOwnerFlags.SellingHouse
                : (byte)PlayerHouseOwnerFlags.HouseOwner;
        }
        player.BuildingOwnerState = buildingState;

        if (!options.Value.Enable) return;

        await conn.SendAsync(new SM_HOUSE_OWNER_INFO(player, activeHouse), ct);
    }
}
