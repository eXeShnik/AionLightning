using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Broadcasts a megaphone message to all or same-faction players. Opcode 0x11D.</summary>
public sealed class SM_MEGAPHONE : AionServerPacket
{
    private readonly string _senderName;
    private readonly string _message;
    private readonly int    _itemId;
    private readonly bool   _isAll;
    private readonly Race   _senderRace;

    public SM_MEGAPHONE(string senderName, string message, int itemId, bool isAll, Race senderRace)
        : base(0x11D)
    {
        _senderName = senderName;
        _message    = message;
        _itemId     = itemId;
        _isAll      = isAll;
        _senderRace = senderRace;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteS(_senderName);
        w.WriteS(_message);
        w.WriteD(_itemId);
        // 255 = faction-scoped; sender race id = cross-faction ("all") broadcast
        w.WriteC(_isAll ? (byte)_senderRace : (byte)255);
    }
}
