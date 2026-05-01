using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client breaks apart a fused weapon, removing the secondary fusion. Opcode 0x16D.
/// Clears FusionedItemId on the primary weapon and persists.
/// Mirrors Java ArmsfusionService.breakWeapons.
/// </summary>
public sealed class CM_BREAK_WEAPONS : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IItemDao           _itemDao;

    private int _unk;
    private int _weaponObjId;

    public CM_BREAK_WEAPONS(GsClientConnection conn, IItemDao itemDao)
    {
        _conn    = conn;
        _itemDao = itemDao;
    }

    public override void Read(ref PacketReader r)
    {
        _unk         = r.ReadD();
        _weaponObjId = r.ReadD();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var weapon = player.Inventory.Get(_weaponObjId);
        if (weapon is null) return;

        if (weapon.FusionedItemId == 0)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.DecompoundNotAvailable(), ct);
            return;
        }

        weapon.FusionedItemId = 0;
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([weapon]), ct);
        await _conn.SendAsync(SM_SYSTEM_MESSAGE.DecompoundSuccess(), ct);
    }
}
