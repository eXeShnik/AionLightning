namespace AionLightning.Game.Model.Item;

public sealed class Item
{
    public long UniqueId     { get; init; }
    public int  ItemId       { get; init; }
    public long Count        { get; set; }
    public int  Slot         { get; set; } = -1;     // equipment slot bitmask when IsEquipped; bag position otherwise
    public byte StorageType  { get; set; } = 0;     // 0 = inventory, 1 = personal warehouse
    public byte EnchantLevel    { get; set; } = 0;      // 0-15
    public int  GodStoneItemId  { get; set; } = 0;      // itemId of socketed godstone (0 = none)
    public int  OptionalSocket  { get; set; } = -1;     // tuning result (−1 = untuned; 0+ = extra socket count from tuning)
    public int  SkinItemId      { get; set; } = 0;      // remodel override (0 = use ItemId for appearance)
    public int  FusionedItemId  { get; set; } = 0;      // fused secondary weapon's ItemId (0 = not fused)
    public bool IsEquipped      { get; set; } = false;  // explicit flag — Slot alone is ambiguous (bitmask vs. bag position)
}
