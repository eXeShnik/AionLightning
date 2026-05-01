using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client answers a yes/no dialog opened by SM_QUESTION_WINDOW. Opcode 0xF0.</summary>
public sealed class CM_QUESTION_RESPONSE : AionClientPacket
{
    private readonly GsClientConnection     _conn;
    private readonly PlayerResponseRegistry _responseRegistry;

    private int  _questionId;
    private byte _response;

    public CM_QUESTION_RESPONSE(GsClientConnection conn, PlayerResponseRegistry responseRegistry)
    {
        _conn             = conn;
        _responseRegistry = responseRegistry;
    }

    public override void Read(ref PacketReader r)
    {
        _questionId = r.ReadD();
        _response   = r.ReadC();
        r.ReadC(); // unk
        r.ReadH(); // unk
        r.ReadD(); // sender objectId
        r.ReadD(); // unk
        r.ReadH(); // unk
    }

    public override ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is not null)
            _responseRegistry.Respond(player.ObjectId, _response == 1);
        return ValueTask.CompletedTask;
    }
}
