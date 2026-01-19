using System.Buffers;
using System.Security.Cryptography;
using AionLightning.Login.Controller;
using AionLightning.Login.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;
using AionLightning.Login.Configs;
using AionLightning.Login.Network.Aion;
using AionLightning.Commons.Utils;

namespace AionLightning.Login.Network.Aion.ClientPackets;

public class CM_LOGIN : AionClientPacket
{
    private byte[] _data;

    public CM_LOGIN(ILogger<CM_LOGIN> logger, ReadOnlySequence<byte> buffer, LoginConnection client) : base(logger, buffer, client)
    {
    }

    protected override void ReadImpl()
    {
        var reader = new SequenceReader<byte>(_buffer);
        reader.Advance(4);
        _data = reader.ReadBytes(128).ToArray();
    }

    protected override void RunImpl()
    {
        if (_data == null)
            return;

        byte[] decrypted;
        try
        {
            var rsa = Connection.RsaKeyPair;
            decrypted = rsa.Decrypt(_data, RSAEncryptionPadding.Pkcs1);
        }
        catch (CryptographicException e)
        {
            _logger.LogError(e, "Error decrypting login data");
            Connection.Close(new SM_LOGIN_FAIL(AionAuthResponse.SYSTEM_ERROR), false);
            return;
        }

        var user = ByteUtils.GetString(decrypted, 64, 32).Trim().ToLower();
        var password = ByteUtils.GetString(decrypted, 96, 32).Trim();

        var response = AccountController.Login(user, password, Connection);

        switch (response)
        {
            case AionAuthResponse.OK:
                Connection.Account.LastIp = Connection.Ip;
                Connection.State = LoginConnection.LoginState.AUTHED_LOGIN;
                Connection.SendPacket(new SM_LOGIN_OK(Connection.SessionKey));
                break;
            default:
                Connection.Close(new SM_LOGIN_FAIL(response), false);
                break;
        }
    }
}