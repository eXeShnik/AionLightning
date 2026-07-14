using AionLightning.Commons.Network;
using AionLightning.Game.Model.House;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Java network.aion.serverpackets.SM_HOUSE_BIDS — one page (max 181 entries, Java's ListSplitter chunk
/// size) of the house-auction list, plus the requesting player's own current bid and any house of theirs
/// currently up for sale. Opcode 0x100 (4.5-era packet table, per the task spec). TODO: verify opcode
/// against a live 4.6 client capture before enabling — the same 0x100 collides with this port's
/// CM_EXCHANGE_ADD_KINAH *client* opcode, which is a different opcode space (client-&gt;server vs
/// server-&gt;client) so no runtime conflict, but it underlines that this value is unverified.
/// </summary>
public sealed class SM_HOUSE_BIDS : AionServerPacket
{
    private readonly bool _isFirst;
    private readonly bool _isLast;
    private readonly HouseBidEntry? _playerBid;
    private readonly IReadOnlyList<HouseBidEntry> _houseBids;
    private readonly HouseBidEntry? _sellEntry;
    private readonly int _secondsTillAuction;
    private readonly Func<HouseBidEntry, bool> _canBid;

    public SM_HOUSE_BIDS(
        bool isFirstPacket,
        bool isLastPacket,
        HouseBidEntry? playerBid,
        IReadOnlyList<HouseBidEntry> houseBids,
        HouseBidEntry? sellEntry,
        int secondsTillAuction,
        Func<HouseBidEntry, bool> canBid) : base(0x100)
    {
        _isFirst = isFirstPacket;
        _isLast = isLastPacket;
        _playerBid = playerBid;
        _houseBids = houseBids;
        _sellEntry = sellEntry;
        _secondsTillAuction = secondsTillAuction;
        _canBid = canBid;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC((byte)(_isFirst ? 1 : 0));
        w.WriteC((byte)(_isLast ? 1 : 0));

        if (_playerBid is null)
        {
            w.WriteD(0);
            w.WriteQ(0);
        }
        else
        {
            w.WriteD(_playerBid.EntryIndex);
            w.WriteQ(_playerBid.BidPrice);
        }

        if (_sellEntry is null)
        {
            w.WriteD(0);
            w.WriteQ(0);
        }
        else
        {
            w.WriteD(_sellEntry.EntryIndex);
            w.WriteQ(_sellEntry.BidPrice);
        }

        w.WriteH((short)_houseBids.Count);
        foreach (var entry in _houseBids)
        {
            w.WriteD(entry.EntryIndex);
            w.WriteD(entry.LandId);
            w.WriteD(entry.Address);
            w.WriteD(entry.BuildingId);
            if (_sellEntry is not null && entry.EntryIndex == _sellEntry.EntryIndex)
                w.WriteD(0);
            else
                w.WriteD(_canBid(entry) ? HouseTypeId(entry.HouseType) : 0);
            w.WriteQ(entry.BidPrice);
            w.WriteQ(entry.Unk2);
            w.WriteD(entry.BidCount);
            w.WriteD(_secondsTillAuction);
        }
    }

    // Java model.templates.housing.HouseType.getId() — a client-protocol id distinct from the enum's
    // declaration order (ESTATE=3, MANSION=2, HOUSE=1, STUDIO=0, PALACE=4).
    private static int HouseTypeId(Model.Templates.Housing.HouseType type) => type switch
    {
        Model.Templates.Housing.HouseType.ESTATE => 3,
        Model.Templates.Housing.HouseType.MANSION => 2,
        Model.Templates.Housing.HouseType.HOUSE => 1,
        Model.Templates.Housing.HouseType.STUDIO => 0,
        Model.Templates.Housing.HouseType.PALACE => 4,
        _ => 0,
    };
}
