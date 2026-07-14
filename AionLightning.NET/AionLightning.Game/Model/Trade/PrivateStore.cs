namespace AionLightning.Game.Model.Trade;

/// <summary>
/// A player's active personal shop — port of Java model.gameobjects.player.PrivateStore.
/// Created when the owner first submits an item list (<see cref="PrivateStoreItem"/> mirrors
/// Java's TradePSItem) and cleared entirely when the shop closes (see PrivateStoreService).
/// </summary>
public sealed class PrivateStore
{
    public Player Owner { get; }
    public string Name  { get; set; } = string.Empty;

    public List<PrivateStoreItem> Items { get; } = new();

    public PrivateStore(Player owner) => Owner = owner;
}
