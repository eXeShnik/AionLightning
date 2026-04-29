namespace AionLightning.Game.Model.Item;

public sealed class Item
{
    public long UniqueId   { get; init; }
    public int  ItemId     { get; init; }
    public long Count      { get; set; }
    public int  Slot       { get; set; }    // -1 = not equipped
    public bool IsEquipped => Slot >= 0;
}
