using AionLightning.Commons.Network;
using AionLightning.Login.Controller;
using AionLightning.Login.Model;
using AionLightning.Login.Network.GameServer.ServerPackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login.Network.GameServer.Clientpackets;

/// <summary>
/// Game server asks for a reconnect key: the player is leaving the GS back to
/// server select. The account moves from the GS list into the reconnecting list.
/// </summary>
public sealed class CM_ACCOUNT_RECONNECT_KEY : GsClientPacket
{
    private readonly GsConnection _conn;
    private readonly IAccountController _accountCtrl;
    private int _accountId;

    public CM_ACCOUNT_RECONNECT_KEY(GsConnection conn, IAccountController accountCtrl)
    {
        _conn = conn;
        _accountCtrl = accountCtrl;
    }

    public override void Read(ref PacketReader r)
    {
        _accountId = r.ReadD();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        int reconnectKey = Random.Shared.Next();

        var gsi = _conn.GameServerInfo;
        var account = gsi?.GetAccount(_accountId);
        if (account == null)
        {
            _conn.Log.LogWarning("CM_ACCOUNT_RECONNECT_KEY for account {Id} not present on GS #{Gs}",
                _accountId, gsi?.Id);
        }
        else
        {
            gsi!.RemoveAccount(account);
            _accountCtrl.AddReconnectingAccount(new ReconnectingAccount(account, reconnectKey));
        }

        await _conn.SendAsync(new SM_ACCOUNT_RECONNECT_KEY(_accountId, reconnectKey), ct);
    }
}
