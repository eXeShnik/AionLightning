using AionLightning.Commons.Network;
using AionLightning.Game.Model.Summons;
using AionLightning.Game.Services;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client orders its summon to attack a specific target. Opcode 0x169.</summary>
public sealed class CM_SUMMON_ATTACK : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly GameWorld _world;
    private readonly SummonsService _summonsService;

    private int _summonObjId;
    private int _targetObjId;

    public CM_SUMMON_ATTACK(GsClientConnection conn, GameWorld world, SummonsService summonsService)
    {
        _conn           = conn;
        _world          = world;
        _summonsService = summonsService;
    }

    public override void Read(ref PacketReader r)
    {
        _summonObjId = r.ReadD();
        _targetObjId = r.ReadD();
        r.ReadC(); // unk
        r.ReadH(); // time
        r.ReadC(); // unk
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var summon = _conn.ActivePlayer?.Summon;
        if (summon is null || summon.ObjectId != _summonObjId) return;

        if (_world.GetNpcByObjectId(_targetObjId) is null) return;

        await _summonsService.DoModeAsync(summon, SummonMode.Attack, _targetObjId, ct);
    }
}
