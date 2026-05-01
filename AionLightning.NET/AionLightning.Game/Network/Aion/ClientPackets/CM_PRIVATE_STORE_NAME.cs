using AionLightning.Commons.Network;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Seller sets shop name and opens the private store. Opcode 0x15A.</summary>
public sealed class CM_PRIVATE_STORE_NAME : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;

    private string _name = string.Empty;

    public CM_PRIVATE_STORE_NAME(GsClientConnection conn, PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r) => _name = r.ReadS();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null || player.IsAlreadyDead) return;

        player.StoreName  = _name;
        player.StoreItems ??= [];
        player.State      |= CreatureState.PrivateShop;

        int worldId    = player.Position.WorldId;
        var openEmotion = new SM_EMOTION(player, EmotionType.OPEN_PRIVATESHOP);
        var namePacket  = new SM_PRIVATE_STORE_NAME(player.ObjectId, _name);

        foreach (var conn in _connRegistry.GetAll())
        {
            if (conn.ActivePlayer?.Position.WorldId != worldId) continue;
            try
            {
                await conn.SendAsync(openEmotion, ct);
                await conn.SendAsync(namePacket, ct);
            }
            catch { }
        }
    }
}
