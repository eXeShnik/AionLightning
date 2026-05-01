namespace AionLightning.Game.Model.Item;

public sealed class Item
{
    public long UniqueId     { get; init; }
    public int  ItemId       { get; init; }
    public long Count        { get; set; }
    public int  Slot         { get; set; } = -1;     // equipment slot bitmask when IsEquipped; bag position otherwise
    public byte StorageType  { get; set; } = 0;     // 0 = inventory, 1 = personal warehouse
    public byte EnchantLevel    { get; set; } = 0;     // 0-15
    public int  GodStoneItemId  { get; set; } = 0;     // itemId of socketed godstone (0 = none)
    public bool IsEquipped      { get; set; } = false; // explicit flag — Slot alone is ambiguous (bitmask vs. bag position)
}
