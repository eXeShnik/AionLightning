namespace AionLightning.Game.Model.House;

/// <summary>
/// Java model.gameobjects.player.PlayerHouseOwnerFlags — bit flags packed into
/// <see cref="Player.BuildingOwnerState"/> (login owner-status byte sent in SM_HOUSE_OWNER_INFO) and,
/// in a later phase, into House.HouseOwnerInfoFlags. Several members alias the same bit under a
/// different name depending on which of the two contexts reads it (e.g. HasOwner/IsOwner both = 1),
/// exactly as in the Java enum.
/// </summary>
[Flags]
public enum PlayerHouseOwnerFlags : byte
{
    IsOwner = 1,
    HasOwner = 1,
    BuyStudioAllowed = 1 << 1,
    SingleHouse = 1 << 1,
    BiddingAllowed = 1 << 2,
    HouseOwner = (IsOwner | BiddingAllowed) & ~BuyStudioAllowed,
    SellingHouse = IsOwner | BuyStudioAllowed,
    // Player status
    SoldHouse = BiddingAllowed | BuyStudioAllowed,
}
