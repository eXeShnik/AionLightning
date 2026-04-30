using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client searches for a player by name/class/level/region filters. Opcode 0x17D.</summary>
public sealed class CM_PLAYER_SEARCH : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;

    private string _name = string.Empty;
    private int    _region;
    private int    _classMask;
    private byte   _minLevel;
    private byte   _maxLevel;
    private byte   _lfgOnly;

    public CM_PLAYER_SEARCH(GsClientConnection conn, PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r)
    {
        var nameBytes = r.ReadB(52 * 2); // fixed-width UTF-16LE, 52 chars
        int nullIdx = 0;
        while (nullIdx + 1 < nameBytes.Length && (nameBytes[nullIdx] != 0 || nameBytes[nullIdx + 1] != 0))
            nullIdx += 2;
        _name = System.Text.Encoding.Unicode.GetString(nameBytes, 0, nullIdx);
        _region    = r.ReadD();
        _classMask = r.ReadD();
        _minLevel  = (byte)r.ReadC();
        _maxLevel  = (byte)r.ReadC();
        _lfgOnly   = (byte)r.ReadC();
        r.ReadC(); // padding
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var searcher = _conn.ActivePlayer;
        if (searcher is null) return;

        var results = _connRegistry.GetAll()
            .Select(c => c.ActivePlayer)
            .Where(p => p is not null && p.ObjectId != searcher.ObjectId)
            .Cast<Model.Player>()
            .Where(p =>
            {
                if (!string.IsNullOrEmpty(_name) &&
                    !p.Name.Contains(_name, StringComparison.OrdinalIgnoreCase)) return false;
                if (_region != 0 && p.Position.WorldId != _region) return false;
                if (_classMask != 0 && (_classMask & (1 << (int)p.PlayerClass)) == 0) return false;
                if (_minLevel != 0xFF && p.Level < _minLevel) return false;
                if (_maxLevel != 0xFF && p.Level > _maxLevel) return false;
                if (_lfgOnly == 1 && p.Group is not null) return false;
                return true;
            })
            .Take(30);

        await _conn.SendAsync(new SM_PLAYER_SEARCH(results), ct);
    }
}
