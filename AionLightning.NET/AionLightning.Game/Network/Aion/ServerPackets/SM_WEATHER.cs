using AionLightning.Commons.Network;
using AionLightning.Game.Model.World;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Java network.aion.serverpackets.SM_WEATHER — the current weather code per zone index of the
/// player's map. Opcode 0x43 (4.5-era packet table). TODO: verify opcode against a live 4.6 client
/// capture before enabling — this port's own SM_GROUP_INFO already occupies 0x43 (pre-existing
/// collision in this codebase's opcode table, same caveat as SM_HOUSE_BIDS' 0x100 note), so
/// <see cref="Configs.Options.WeatherOptions"/> keeps the client send gated off by default.
/// </summary>
public sealed class SM_WEATHER : AionServerPacket
{
    private readonly IReadOnlyList<WeatherEntry> _entries;

    public SM_WEATHER(IReadOnlyList<WeatherEntry> entries) : base(0x43) => _entries = entries;

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(0x00); // unk
        w.WriteC((byte)_entries.Count);
        foreach (var entry in _entries)
            w.WriteC((byte)entry.Code);
    }
}
