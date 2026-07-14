using AionLightning.Commons.Network;
using AionLightning.Game.Model.GameObjects;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Java network.aion.serverpackets.SM_HOUSE_EDIT — every CM_HOUSE_EDIT/CM_HOUSE_DECORATE acknowledgement.
/// Java resolves <c>itemObjectId</c> against <c>con.getActivePlayer().getHouseRegistry()</c> inside
/// writeImpl; this port's packets never see the connection at write time (see AionServerPacket.Write), so
/// the caller passes the already-resolved <see cref="HouseObject"/>/<see cref="HouseDecoration"/> straight
/// in instead of an id to look up again.
/// Opcode 0x52 (4.5-era packet table). TODO: verify opcode vs live 4.6 client.
/// </summary>
public sealed class SM_HOUSE_EDIT : AionServerPacket
{
    private readonly int _action;
    private readonly int _storeId;
    private readonly int _itemObjectId;
    private readonly HouseObject? _obj;
    private readonly HouseDecoration? _decor;
    private readonly float _x, _y, _z;
    private readonly int _rotation;
    private readonly int _reuseDelaySeconds;
    private readonly int _playerObjectId;
    private readonly int _houseOwnerId;

    /// <summary>Action-only ack — ENTER_DECORATION(1)/EXIT_DECORATION(2)/ENTER_RENOVATION(14)/
    /// EXIT_RENOVATION(15), or any other bare action byte.</summary>
    public SM_HOUSE_EDIT(int action) : base(0x52)
    {
        _action = action;
    }

    /// <summary>"Add item" (action 3, storeId 1=object/2=decoration), "remove from inventory" (action 4)
    /// and "despawn object" (action 7) acks. <paramref name="obj"/>/<paramref name="decor"/> resolve the
    /// templateId/typeId/usage-data tail action 3 needs (both null is fine for actions 4/7, which don't
    /// read them).</summary>
    public SM_HOUSE_EDIT(int action, int storeId, int itemObjectId, HouseObject? obj = null, HouseDecoration? decor = null)
        : base(0x52)
    {
        _action = action;
        _storeId = storeId;
        _itemObjectId = itemObjectId;
        _obj = obj;
        _decor = decor;
    }

    /// <summary>"Spawn or move object" (action 5/6 — Java writes action±1 for the two intermediate
    /// despawn/respawn animation frames, both funnel through this same wire shape).</summary>
    public SM_HOUSE_EDIT(int action, HouseObject obj, float x, float y, float z, int rotation,
        int reuseDelaySeconds, int playerObjectId, int houseOwnerId) : base(0x52)
    {
        _action = action;
        _obj = obj;
        _itemObjectId = obj.ObjectId;
        _x = x;
        _y = y;
        _z = z;
        _rotation = rotation;
        _reuseDelaySeconds = reuseDelaySeconds;
        _playerObjectId = playerObjectId;
        _houseOwnerId = houseOwnerId;
    }

    public override void Write(ref PacketWriter w)
    {
        switch (_action)
        {
            case 3:
                WriteAddItem(ref w);
                break;
            case 4:
                w.WriteC((byte)_action);
                w.WriteC((byte)_storeId);
                w.WriteD(_itemObjectId);
                break;
            case 5:
                WriteSpawnOrMove(ref w);
                break;
            case 7:
                w.WriteC((byte)_action);
                w.WriteD(_itemObjectId);
                break;
            default:
                w.WriteC((byte)_action);
                break;
        }
    }

    private void WriteAddItem(ref PacketWriter w)
    {
        int templateId = _obj?.TemplateId ?? _decor?.PartId ?? 0;
        byte typeId = _obj?.TypeId ?? 0;

        w.WriteC((byte)_action);
        w.WriteC((byte)_storeId);
        w.WriteD(_itemObjectId);
        w.WriteD(templateId);
        w.WriteD(_obj is { } o && o.UseSecondsLeft > 0 ? o.UseSecondsLeft : 0);
        HouseObjectPacketBody.WriteColor(ref w, _obj?.Color);
        w.WriteD(0); // expiration as for armor
        w.WriteC(typeId);

        if (_obj is { TypeId: 1 } useObj)
        {
            w.WriteD(_playerObjectId);
            HouseObjectPacketBody.WriteUseItemUsageData(ref w, useObj);
        }
    }

    private void WriteSpawnOrMove(ref PacketWriter w)
    {
        var obj = _obj!;
        w.WriteC((byte)_action);
        w.WriteD(_houseOwnerId);
        w.WriteD(_playerObjectId);
        w.WriteD(_itemObjectId);
        w.WriteD(obj.TemplateId);
        w.WriteF(_x);
        w.WriteF(_y);
        w.WriteF(_z);
        w.WriteH(_rotation);
        w.WriteD(_reuseDelaySeconds);
        w.WriteD(obj.UseSecondsLeft > 0 ? obj.UseSecondsLeft : 0);
        HouseObjectPacketBody.WriteColor(ref w, obj.Color);
        w.WriteD(0); // expiration as for armor
        w.WriteC(obj.TypeId);
        if (obj.TypeId == 1)
            HouseObjectPacketBody.WriteUseItemUsageData(ref w, obj);
    }
}
