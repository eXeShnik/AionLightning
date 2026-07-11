using AionLightning.Commons.Network;
using AionLightning.Game.Model.Summons;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client switches its summon's mode (GUARD/ATTACK/REST/RELEASE). Opcode 0x15B.</summary>
public sealed class CM_SUMMON_COMMAND : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly SummonsService _summonsService;

    private int _mode;
    private int _targetObjId;

    public CM_SUMMON_COMMAND(GsClientConnection conn, SummonsService summonsService)
    {
        _conn           = conn;
        _summonsService = summonsService;
    }

    public override void Read(ref PacketReader r)
    {
        _mode = r.ReadC();
        r.ReadD(); // unk1
        r.ReadD(); // unk2
        _targetObjId = r.ReadD();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var summon = _conn.ActivePlayer?.Summon;
        if (summon is null) return;
        if (!Enum.IsDefined(typeof(SummonMode), _mode)) return;

        await _summonsService.DoModeAsync(summon, (SummonMode)_mode, _targetObjId, ct);
    }
}
