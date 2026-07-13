namespace AionLightning.Game.Model.House;

/// <summary>
/// Persisted housing-plot state (Java model.house.House, without the VisibleObject/spawn/registry
/// behavior — that belongs to the future housing service/spawn layer). One row per house regardless of
/// whether it currently has an owner. <see cref="Id"/> is the house's own persisted id (Java assigns it
/// from IDFactory); <see cref="Address"/> is the <c>HouseAddress.Id</c> template it occupies.
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

    public const int NoticeLength = 130;

    public bool IsOwned => PlayerObjectId != 0;
}
