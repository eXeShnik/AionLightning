using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_RECIPE_DELETE : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IRecipeDao         _recipeDao;
    private int _recipeId;

    public CM_RECIPE_DELETE(GsClientConnection conn, IRecipeDao recipeDao)
    {
        _conn      = conn;
        _recipeDao = recipeDao;
    }

    public override void Read(ref PacketReader r) => _recipeId = r.ReadD();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        player.KnownRecipes.Remove(_recipeId);
        await _recipeDao.DeleteRecipeAsync(player.ObjectId, _recipeId, ct);
        await _conn.SendAsync(new SM_RECIPE_LIST(player.KnownRecipes), ct);
    }
}
