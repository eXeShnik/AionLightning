using AionLightning.Commons.Network;
using AionLightning.Game.Model.Broker;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Multiplexed broker (auction house) response. Opcode 0x92.</summary>
public sealed class SM_BROKER_SERVICE : AionServerPacket
{
    private enum BrokerType : byte
    {
        SearchedItems   = 0,
        RegisteredItems = 1,
        BuyResult       = 2,
        RegisterResult  = 3,
        SettledItems    = 5,
        RemoveSettledIcon = 6,
    }

    private readonly BrokerType              _type;
    private readonly IReadOnlyList<BrokerItem> _items;
    private readonly int                     _totalCount;
    private readonly int                     _page;
    private readonly long                    _kinah;
    private readonly int                     _message;
    private readonly int                     _newListingCount;

    private SM_BROKER_SERVICE(BrokerType type,
        IReadOnlyList<BrokerItem>? items = null,
        int totalCount = 0, int page = 0,
        long kinah = 0, int message = 0, int newListingCount = 0)
        : base(0x92)
    {
        _type            = type;
        _items           = items ?? Array.Empty<BrokerItem>();
        _totalCount      = totalCount;
        _page            = page;
        _kinah           = kinah;
        _message         = message;
        _newListingCount = newListingCount;
    }

    public static SM_BROKER_SERVICE SearchedItems(IReadOnlyList<BrokerItem> items, int totalCount, int page)
        => new(BrokerType.SearchedItems, items, totalCount: totalCount, page: page);

    public static SM_BROKER_SERVICE RegisteredItems(IReadOnlyList<BrokerItem> items)
        => new(BrokerType.RegisteredItems, items);

    public static SM_BROKER_SERVICE BuyResult(long remainingKinah)
        => new(BrokerType.BuyResult, kinah: remainingKinah);

    public static SM_BROKER_SERVICE RegisterSuccess(BrokerItem item, int newListingCount)
        => new(BrokerType.RegisterResult, [item], newListingCount: newListingCount);

    public static SM_BROKER_SERVICE RegisterError(int errorCode)
        => new(BrokerType.RegisterResult, message: errorCode);

    public static SM_BROKER_SERVICE SettledItems(IReadOnlyList<BrokerItem> items, long settledKinah)
        => new(BrokerType.SettledItems, items, kinah: settledKinah);

    public static SM_BROKER_SERVICE ShowSettledIcon(long settledKinah)
        => new(BrokerType.SettledItems, [], kinah: settledKinah);

    public static SM_BROKER_SERVICE RemoveSettledIcon()
        => new(BrokerType.RemoveSettledIcon);

    public override void Write(ref PacketWriter w)
    {
        switch (_type)
        {
            case BrokerType.SearchedItems:
                WriteSearchedItems(ref w);
                break;
            case BrokerType.RegisteredItems:
                WriteRegisteredItems(ref w);
                break;
            case BrokerType.BuyResult:
                WriteBuyResult(ref w);
                break;
            case BrokerType.RegisterResult:
                WriteRegisterResult(ref w);
                break;
            case BrokerType.SettledItems:
                WriteSettledItems(ref w);
                break;
            case BrokerType.RemoveSettledIcon:
                w.WriteH((short)BrokerType.RemoveSettledIcon);
                break;
        }
    }

    // ──────────────────── SEARCHED_ITEMS ────────────────────
    private void WriteSearchedItems(ref PacketWriter w)
    {
        w.WriteC((byte)BrokerType.SearchedItems);
        w.WriteD(_totalCount);
        w.WriteC(0);
        w.WriteH((short)_page);
        w.WriteH((short)_items.Count);
        foreach (var item in _items)
            WriteBrokerItemInfo(ref w, item);
    }

    // ──────────────────── REGISTERED_ITEMS ────────────────────
    private void WriteRegisteredItems(ref PacketWriter w)
    {
        w.WriteC((byte)BrokerType.RegisteredItems);
        w.WriteD(0);
        w.WriteH((short)_items.Count);
        foreach (var item in _items)
            WriteRegisteredItemInfo(ref w, item);
    }

    // ──────────────────── BUY_RESULT ────────────────────
    private void WriteBuyResult(ref PacketWriter w)
    {
        w.WriteC((byte)BrokerType.BuyResult);
        w.WriteC(0);
        w.WriteD(1);  // item count bought (always 1 stack)
        w.WriteQ(_kinah);
        w.WriteD(0);
        w.WriteC(0);
    }

    // ──────────────────── REGISTER_RESULT ────────────────────
    private void WriteRegisterResult(ref PacketWriter w)
    {
        w.WriteC((byte)BrokerType.RegisterResult);
        w.WriteC((byte)_message);
        if (_message == 0 && _items.Count > 0)
        {
            w.WriteC((byte)_newListingCount);
            WriteRegisteredItemInfo(ref w, _items[0]);
        }
        else
        {
            // 107 bytes padding for error case
            for (int i = 0; i < 107; i++) w.WriteC(0);
        }
    }

    // ──────────────────── SETTLED_ITEMS ────────────────────
    private void WriteSettledItems(ref PacketWriter w)
    {
        w.WriteC((byte)BrokerType.SettledItems);
        w.WriteQ(_kinah);
        w.WriteH((short)_items.Count);
        w.WriteD(0);
        w.WriteC(0);
        w.WriteH((short)_items.Count);
        foreach (var item in _items)
        {
            w.WriteD(item.ItemId);
            w.WriteQ(item.IsSold ? item.Price : 0);
            w.WriteQ(item.ItemCount);
            w.WriteQ(item.ItemCount);
            int settleMinutes = item.SettleTime.HasValue
                ? (int)(item.SettleTime.Value.Ticks / TimeSpan.TicksPerMinute)
                : 0;
            w.WriteD(settleMinutes);
            WriteManaSocketsBlob(ref w, item);
            w.WriteS(item.CreatorName);
        }
    }

    // ──────────────────── Per-item helpers ────────────────────

    private static void WriteBrokerItemInfo(ref PacketWriter w, BrokerItem item)
    {
        w.WriteD(item.Id);
        w.WriteD(item.ItemId);
        w.WriteQ(item.Price);
        w.WriteQ(item.ItemCount);
        WriteManaSocketsBlob(ref w, item);
        w.WriteS(item.SellerName);
        w.WriteS(item.CreatorName);
        WritePremiumOptionBlob(ref w);
        w.WriteC(0);
        WritePolishInfoBlob(ref w);
    }

    private static void WriteRegisteredItemInfo(ref PacketWriter w, BrokerItem item)
    {
        w.WriteD(item.Id);
        w.WriteD(item.ItemId);
        w.WriteQ(item.Price);
        w.WriteQ(item.ItemCount);
        w.WriteQ(item.ItemCount);
        int daysLeft = Math.Max(0, (int)(item.ExpireTime - DateTime.UtcNow).TotalDays);
        w.WriteC((byte)daysLeft);
        WriteManaSocketsBlob(ref w, item);
        w.WriteS(item.CreatorName);
        WritePremiumOptionBlob(ref w);
        w.WriteC(0);
        WritePolishInfoBlob(ref w);
    }

    // MANA_SOCKETS blob — 132 bytes (mirrors Java ManaStoneInfoBlobEntry.getSize() == 132)
    private static void WriteManaSocketsBlob(ref PacketWriter w, BrokerItem item)
    {
        w.WriteC(0);                   // isSoulBound
        w.WriteC(item.EnchantLevel);   // enchantLevel
        w.WriteD(item.ItemId);         // skinTemplateId (use ItemId as skin — no separate skin stored in broker)
        w.WriteC(0);                   // optionalSocket
        w.WriteC(0); w.WriteC(0);      // unk × 2
        for (int i = 0; i < 12; i++) w.WriteD(0); // 12 manastone slots × 4 = 48 bytes
        w.WriteD(0);                   // godstoneItemId
        w.WriteC(0);                   // dyeFlag
        w.WriteD(0);                   // dyeColor
        w.WriteD(0);                   // dyeUnk
        w.WriteD(0);                   // dyeExpiration
        w.WriteD(0);                   // idianStoneItemId
        w.WriteC(0);                   // idianPolishNumber
        w.WriteC(0);                   // authorize
        w.WriteD(0);                   // unk
        for (int i = 0; i < 12; i++) w.WriteD(0); // 48 bytes non-feather padding
    }

    // PREMIUM_OPTION blob — 3 bytes
    private static void WritePremiumOptionBlob(ref PacketWriter w)
    {
        w.WriteC(0); // bonusNumber
        w.WriteC(0); // randomCount
        w.WriteC(0); // unk
    }

    // POLISH_INFO blob — 4 bytes
    private static void WritePolishInfoBlob(ref PacketWriter w)
    {
        w.WriteD(0); // polishCharge
    }
}
