using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends a slice of the player's macro list. Opcode 0xE7.</summary>
public sealed class SM_MACRO_LIST : AionServerPacket
{
    private readonly int _playerObjectId;
    private readonly IReadOnlyList<KeyValuePair<int, string>> _macros;

    public SM_MACRO_LIST(int playerObjectId, IEnumerable<KeyValuePair<int, string>>? macros = null)
        : base(0xE7)
    {
        _playerObjectId = playerObjectId;
        _macros = macros?.ToList() ?? [];
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_playerObjectId);
        w.WriteC(0);
        w.WriteH((short)-_macros.Count); // protocol sends negated count

        foreach (var (position, xml) in _macros)
        {
            w.WriteC((byte)position);
            w.WriteS(xml);
        }
    }
}
