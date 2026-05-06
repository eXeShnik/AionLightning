using AionLightning.Commons.Network;

namespace AionLightning.Login.Network.Aion.ServerPackets;

public sealed class SM_SERVER_LIST : AionServerPacket
{
    private readonly IEnumerable<GameServerInfo> _servers;
    private readonly sbyte _lastServer;
    private readonly string _playerIp;

    public SM_SERVER_LIST(IEnumerable<GameServerInfo> servers, sbyte lastServer, string playerIp) : base(0x04)
    {
        _servers = servers;
        _lastServer = lastServer;
        _playerIp = playerIp;
    }

    public override void Write(ref PacketWriter w)
    {
        var list = _servers.ToList();
        w.WriteC((byte)list.Count);
        w.WriteC((byte)_lastServer);

        int maxId = 0;
        foreach (var gsi in list)
        {
            w.WriteC(gsi.Id);
            w.WriteB(gsi.GetIpAddressForPlayer(_playerIp));
            w.WriteD(gsi.Port);
            w.WriteC(0x00);                       // ageLimit
            w.WriteC(0x01);                       // pvp
            w.WriteH((short)gsi.GetCurrentPlayers());
            w.WriteH((short)gsi.MaxPlayers);
            w.WriteC((byte)(gsi.IsOnline ? 1 : 0));
            w.WriteD(1);                          // char race bits (matches Java: writeD(1))
            w.WriteC(1);                          // brackets (matches Java: writeC(1))
            if (gsi.Id > maxId) maxId = gsi.Id;
        }

        // character counts per server slot 1..maxId (Java: for i=1; i<=maxId)
        w.WriteH((short)(maxId + 1));
        w.WriteC(0x01);
        for (int i = 1; i <= maxId; i++)
            w.WriteC(0x00);
    }
}
