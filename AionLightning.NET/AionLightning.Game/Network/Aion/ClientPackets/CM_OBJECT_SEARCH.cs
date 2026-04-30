using AionLightning.Commons.Network;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client requests to find an NPC by ID and show its location on the minimap. Opcode 0xA9.
/// Responds with SM_SHOW_NPC_ON_MAP when the NPC has a spawn entry.
/// </summary>
public sealed class CM_OBJECT_SEARCH : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IDataManager       _dataManager;

    private int _npcId;

    public CM_OBJECT_SEARCH(GsClientConnection conn, IDataManager dataManager)
    {
        _conn        = conn;
        _dataManager = dataManager;
    }

    public override void Read(ref PacketReader r) => _npcId = r.ReadD();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        if (_conn.ActivePlayer is null) return;

        var result = _dataManager.Spawns.GetFirstSpawnByNpcId(_npcId);
        if (result is not null)
        {
            var (mapId, _, spot) = result.Value;
            await _conn.SendAsync(new SM_SHOW_NPC_ON_MAP(_npcId, mapId, spot.X, spot.Y, spot.Z), ct);
        }
    }
}
