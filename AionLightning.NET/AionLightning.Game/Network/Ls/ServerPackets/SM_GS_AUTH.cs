using System.Net;
using AionLightning.Commons.Network;
using AionLightning.Game.Configs.Options;

namespace AionLightning.Game.Network.Ls.ServerPackets;

public sealed class SM_GS_AUTH : AionServerPacket
{
    private readonly GameServerInfoOptions _info;
    private readonly NetworkOptions _net;

    public SM_GS_AUTH(GameServerInfoOptions info, NetworkOptions? net = null) : base(0x00)
    {
        _info = info;
        _net = net ?? new NetworkOptions { BindAddress = "0.0.0.0", GamePort = 7777 };
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(_info.Id);

        byte[] defaultAddr = IPAddress.TryParse(_net.BindAddress, out var ip) && !ip.Equals(IPAddress.Any)
            ? ip.GetAddressBytes()
            : IPAddress.Loopback.GetAddressBytes();

        w.WriteC((byte)defaultAddr.Length);
        w.WriteB(defaultAddr);

        w.WriteD(0);    // no IP ranges

        w.WriteH((short)_net.GamePort);
        w.WriteD(_info.MaxPlayers);
        w.WriteS(_info.Password);
    }
}
