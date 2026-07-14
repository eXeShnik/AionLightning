using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Broadcasts a private store's custom name/message to zone players. Opcode 0x9E (4.5-era
/// packet table). TODO: verify opcode against a live 4.6 client capture before enabling.</summary>
public sealed class SM_PRIVATE_STORE_NAME : AionServerPacket
{
    private readonly int    _playerObjectId;
    private readonly string _name;

    public SM_PRIVATE_STORE_NAME(int playerObjectId, string name) : base(0x9E)
    {
        _playerObjectId = playerObjectId;
        _name           = name;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_playerObjectId);
        w.WriteS(_name);
    }
}
