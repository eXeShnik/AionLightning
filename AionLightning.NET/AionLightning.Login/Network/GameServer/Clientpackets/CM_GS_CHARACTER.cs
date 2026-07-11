using AionLightning.Commons.Network;
using AionLightning.Login.Controller;

namespace AionLightning.Login.Network.GameServer.Clientpackets;

/// <summary>
/// Game server's reply to SM_GS_CHARACTER_RESPONSE: character count for an account.
/// </summary>
public sealed class CM_GS_CHARACTER : GsClientPacket
{
    private readonly GsConnection _conn;
    private readonly IAccountController _accountCtrl;
    private int _accountId;
    private byte _characterCount;

    public CM_GS_CHARACTER(GsConnection conn, IAccountController accountCtrl)
    {
        _conn = conn;
        _accountCtrl = accountCtrl;
    }

    public override void Read(ref PacketReader r)
    {
        _accountId = r.ReadD();
        _characterCount = r.ReadC();
    }

    public override ValueTask RunAsync(CancellationToken ct)
    {
        var gsi = _conn.GameServerInfo;
        if (gsi == null) return ValueTask.CompletedTask;
        return new ValueTask(_accountCtrl.AddGsCharacterCountAsync(_accountId, gsi.Id, _characterCount, ct));
    }
}
