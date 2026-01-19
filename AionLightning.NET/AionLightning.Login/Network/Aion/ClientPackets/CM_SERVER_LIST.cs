using System.Buffers;
using AionLightning.Login.Controller;
using AionLightning.Login.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login.Network.Aion.ClientPackets;

public class CM_SERVER_LIST : AionClientPacket
{
    private int _accountId;
    private int _loginOk;

    public CM_SERVER_LIST(ILogger<CM_SERVER_LIST> logger, ReadOnlySequence<byte> buffer, LoginConnection client) : base(logger, buffer, client)
    {
    }

    protected override void ReadImpl()
    {
        var reader = new SequenceReader<byte>(_buffer);
        reader.TryReadLittleEndian(out _accountId);
        reader.TryReadLittleEndian(out _loginOk);
    }

    protected override void RunImpl()
    {
        if (Connection.SessionKey.CheckLogin(_accountId, _loginOk))
        {
            if (GameServerTable.Instance.GameServers.Count == 0)
            {
                Connection.Close(new SM_LOGIN_FAIL(AionAuthResponse.NO_GS_REGISTERED), false);
            }
            else
            {
                // AccountController.LoadGsCharactersCount(_accountId);
                Connection.SendPacket(new SM_SERVER_LIST());
            }
        }
        else
        {
            Connection.Close(new SM_LOGIN_FAIL(AionAuthResponse.SYSTEM_ERROR), false);
        }
    }
}