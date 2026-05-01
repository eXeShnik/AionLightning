namespace AionLightning.Game.Model.Broker;

public sealed class BrokerItem
{
    public int      Id           { get; set; }
    public int      ItemId       { get; set; }
    public int      SellerId     { get; set; }
    public string   SellerName   { get; set; } = string.Empty;
    public string   CreatorName  { get; set; } = string.Empty;
    public long     ItemCount    { get; set; }
    public long     Price        { get; set; }
    public int      Race         { get; set; }  // 0=Elyos, 1=Asmodian
    public byte     EnchantLevel { get; set; }
    public bool     IsSettled    { get; set; }
    public bool     IsSold       { get; set; }
    public bool     IsCanceled   { get; set; }
    public DateTime ExpireTime   { get; set; }
    public DateTime? SettleTime  { get; set; }
}
