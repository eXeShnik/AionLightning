using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_RECIPE_DELETE : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private int _recipeId;

    public CM_RECIPE_DELETE(GsClientConnection conn) { _conn = conn; }

    public override void Read(ref PacketReader r) => _recipeId = r.ReadD();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        player.KnownRecipes.Remove(_recipeId);
        await _conn.SendAsync(new SM_RECIPE_LIST(player.KnownRecipes), ct);
    }
}
