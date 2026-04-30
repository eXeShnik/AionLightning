using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;
namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client closes an NPC dialog window. Sends SM_HEADING_UPDATE to reset NPC facing. Opcode 0x117.</summary>
public sealed class CM_CLOSE_DIALOG : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly World.World        _world;

    private int _targetObjectId;

    public CM_CLOSE_DIALOG(GsClientConnection conn, World.World world)
    {
        _conn  = conn;
        _world = world;
    }

    public override void Read(ref PacketReader r) => _targetObjectId = r.ReadD();

    public override ValueTask RunAsync(CancellationToken ct)
    {
        var npc = _world.GetNpcByObjectId(_targetObjectId);
        if (npc is null) return ValueTask.CompletedTask;

        int npcId   = npc.ObjectId;
        int heading = npc.Position.Heading;

        _ = Task.Run(async () =>
        {
            await Task.Delay(1200, CancellationToken.None);
            try { await _conn.SendAsync(new SM_HEADING_UPDATE(npcId, heading), CancellationToken.None); } catch { }
        }, CancellationToken.None);

        return ValueTask.CompletedTask;
    }
}
