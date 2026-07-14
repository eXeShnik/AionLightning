using AionLightning.Game.Model.Templates.Housing;
using PositionModel = AionLightning.Game.Model.Position;

namespace AionLightning.Game.Model.House;

/// <summary>
/// Persisted housing-plot state (Java model.house.House). One row per house regardless of whether it
/// currently has an owner. <see cref="Id"/> is the house's own persisted id and doubles as its world
/// object id (Java assigns both from the same IDFactory sequence — see HousesDAO.storeHouse, which
/// persists <c>house.getObjectId()</c> as the <c>id</c> column); <see cref="Address"/> is the
/// <c>HouseAddress.Id</c> template it occupies.
///
/// P2 (world-spawn + rendering) adds <see cref="Position"/> (set once <see cref="Services.HousingService.SpawnHouses"/>
/// places the house in its address's world/instance — null before that) and <see cref="Registry"/>
/// (default building-part decorations, populated by the same spawn step). Houses are tracked in
/// HousingService's own dictionaries rather than the shared <c>World</c> NPC/player stores, so spawning
/// them cannot regress the open-world NPC/combat loop (see HousingService.SpawnHouses's doc comment).
/// </summary>
public sealed class House
{
    public int Id { get; set; }
    public int PlayerObjectId { get; set; }
    public int BuildingId { get; set; }
    public int Address { get; set; }
    public DateTime AcquiredTime { get; set; }
    public int Permissions { get; set; }
    public HouseStatus Status { get; set; }
    public bool FeePaid { get; set; } = true;
    public DateTime? NextPay { get; set; }
    public DateTime? SellStarted { get; set; }
    public byte[] SignNotice { get; set; } = new byte[NoticeLength];

    /// <summary>World position once spawned (Java VisibleObject.position, set by SpawnEngine.bringIntoWorld
    /// inside House.spawn). Null until HousingService.SpawnHouses places this house.</summary>
    public PositionModel? Position { get; set; }

    /// <summary>Java House.houseOwnerInfoFlags — bit flags describing ownership/bidding state, sent in the
    /// render packets. Recomputed by <see cref="FixBuildingStates"/>.</summary>
    public byte HouseOwnerInfoFlags { get; private set; } = (byte)PlayerHouseOwnerFlags.SingleHouse;

    private HouseRegistry? _registry;

    /// <summary>Java House.getRegistry() — lazily created. Callers that need default parts populated must
    /// call <see cref="HouseRegistry.LoadDefaultParts"/> explicitly (HousingService.SpawnHouses does this
    /// at spawn time); the model itself has no DataManager access to resolve the owning Building.</summary>
    public HouseRegistry Registry => _registry ??= new HouseRegistry(this);

    public const int NoticeLength = 130;

    public bool IsOwned => PlayerObjectId != 0;

    /// <summary>Java House.getDoorState()/getNoticeState() — thin wrappers over the bit-packed
    /// <see cref="Permissions"/>. Call <see cref="NormalizePermissions"/> first if this house's
    /// permissions might not have been initialized yet (Java's getPermissions() does this on every read;
    /// this port keeps <see cref="Permissions"/> a plain, non-side-effecting property instead).</summary>
    public int DoorState => HousePermissions.GetDoorState(Permissions);
    public int NoticeState => HousePermissions.GetNoticeState(Permissions);

    /// <summary>Java House.getPermissions()'s side-effecting normalize step, factored out as an explicit
    /// method so <see cref="Permissions"/> can stay a plain property. Unowned houses always get door state
    /// re-derived from <see cref="Status"/>; a freshly-owned house (permissions never set) gets the
    /// default "show owner" notice, plus a closed door for PERSONAL_FIELD buildings.</summary>
    public void NormalizePermissions(BuildingType? buildingType)
    {
        if (PlayerObjectId == 0)
        {
            Permissions = HousePermissions.SetDoorState(Permissions,
                Status == HouseStatus.SellWait ? HousePermissions.DOOR_OPENED_ALL : HousePermissions.DOOR_CLOSED);
            Permissions = HousePermissions.SetNoticeState(Permissions, HousePermissions.NOT_SET);
        }
        else if (Permissions == 0)
        {
            Permissions = HousePermissions.SetNoticeState(Permissions, HousePermissions.SHOW_OWNER);
            if (buildingType == BuildingType.PERSONAL_FIELD)
                Permissions = HousePermissions.SetDoorState(Permissions, HousePermissions.DOOR_CLOSED);
        }
    }

    /// <summary>Java House.fixBuildingStates() — recomputes <see cref="HouseOwnerInfoFlags"/> from current
    /// ownership/status. Java calls this from setOwnerId/setStatus; this port calls it explicitly wherever
    /// the flags are about to be read (HouseController.SeeAsync/BroadcastAppearanceAsync,
    /// HousingService.SpawnHouses) instead of hooking it onto property setters.</summary>
    public void FixBuildingStates()
    {
        byte flags = (byte)PlayerHouseOwnerFlags.SingleHouse;
        if (PlayerObjectId != 0)
        {
            flags |= (byte)PlayerHouseOwnerFlags.HasOwner;
            if (Status == HouseStatus.Active)
            {
                flags |= (byte)PlayerHouseOwnerFlags.BiddingAllowed;
                flags &= (byte)~PlayerHouseOwnerFlags.SingleHouse;
            }
        }
        else if (Status == HouseStatus.SellWait)
        {
            flags = (byte)PlayerHouseOwnerFlags.SellingHouse;
        }
        HouseOwnerInfoFlags = flags;
    }
}
