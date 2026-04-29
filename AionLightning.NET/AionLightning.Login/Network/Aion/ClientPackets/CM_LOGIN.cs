using System.Text;
using AionLightning.Commons.Network;
using AionLightning.Commons.Network.Ncrypt;
using AionLightning.Login.Controller;
using AionLightning.Login.Network.Aion.ServerPackets;

namespace AionLightning.Login.Network.Aion.ClientPackets;

public sealed class CM_LOGIN : AionClientPacket
{
    private readonly LoginConnection _conn;
    private readonly IAccountController _accountCtrl;
    private byte[] _rsaData = Array.Empty<byte>();

    public CM_LOGIN(LoginConnection conn, IAccountController accountCtrl)
    {
        _conn = conn;
        _accountCtrl = accountCtrl;
    }

    public override void Read(ref PacketReader r)
    {
        r.Skip(4);          // unused int
        _rsaData = r.ReadB(128);
        r.Skip(4);          // unused int
        r.Skip(31);         // unused bytes
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        byte[] decrypted;
        try
        {
            decrypted = KeyGen.DecryptRSA(_conn.RsaKeyPair.RsaKeyPair, _rsaData);
        }
        catch (Exception)
        {
            await _conn.SendAsync(new SM_LOGIN_FAIL(AionAuthResponse.SYSTEM_ERROR), ct);
            return;
        }

        // Bytes [64..94] = login (null-trimmed, lowercase)
        // Bytes [96..126] = password (null-trimmed)
        string login = Encoding.UTF8.GetString(decrypted, 64, 32).TrimEnd('\0').ToLowerInvariant();
        string password = Encoding.UTF8.GetString(decrypted, 96, 32).TrimEnd('\0');

        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            await _conn.SendAsync(new SM_LOGIN_FAIL(AionAuthResponse.INVALID_PASSWORD), ct);
            return;
        }

        var (response, account) = await _accountCtrl.LoginAsync(login, password, _conn.IP, ct);

        if (response != AionAuthResponse.AUTHED)
        {
            await _conn.SendAsync(new SM_LOGIN_FAIL(response), ct);
            return;
        }

        _conn.Account = account;
        var sk = new SessionKey(account!);
        account!.SessionKey = sk;
        _conn.SessionKey = sk;
        _conn.State = LoginConnection.LoginState.AUTHED_LOGIN;
        _accountCtrl.RegisterAccount(account);

        await _conn.SendAsync(new SM_LOGIN_OK(sk.AccountId, sk.LoginOk), ct);
    }
}
