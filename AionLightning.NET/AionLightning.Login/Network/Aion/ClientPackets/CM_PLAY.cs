using System.Buffers;
using AionLightning.Login.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login.Network.Aion.ClientPackets;

public class CM_PLAY : AionClientPacket
{
    private int _accountId;
    private int _loginOk;
    private byte _serverId;

    public CM_PLAY(ILogger<CM_PLAY> logger, ReadOnlySequence<byte> buffer, LoginConnection client) : base(logger, buffer, client)
    {
    }

    protected override void ReadImpl()
    {
        var reader = new SequenceReader<byte>(_buffer);
        reader.TryReadLittleEndian(out _accountId);
        reader.TryReadLittleEndian(out _loginOk);
        reader.TryRead(out _serverId);
    }

    protected override void RunImpl()
    {
        var key = Connection.SessionKey;
        if (key.CheckLogin(_accountId, _loginOk))
        {
            var gsi = GameServerTable.Instance.GetGameServerInfo(_serverId);
            if (gsi == null || !gsi.IsOnline)
            {
                SendPacket(new SM_PLAY_FAIL(AionAuthResponse.SERVER_DOWN));
            }
            else if (gsi.IsFull())
            {
                SendPacket(new SM_PLAY_FAIL(AionAuthResponse.SERVER_FULL));
            }
            else
            {
                Connection.SetJoinedGs();
                SendPacket(new SM_PLAY_OK(key, _serverId));
            }
        }
        else
        {
            Connection.Close(new SM_LOGIN_FAIL(AionAuthResponse.SYSTEM_ERROR), false);
        }
    }
}