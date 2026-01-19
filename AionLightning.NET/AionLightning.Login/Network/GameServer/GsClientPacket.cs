using AionLightning.Commons.Network;
using AionLightning.Commons.Network.Packet;
using Microsoft.Extensions.Logging;

namespace AionLightning.LoginServer.Network.Gameserver
{
    public abstract class GsClientPacket : BaseClientPacket
    {
        protected GsClientPacket(ByteBuffer buffer, GsConnection client, ILogger<GsClientPacket> log) : base(0, log)
        {
            SetBuffer(buffer.GetRemainingBytes());
            SetClient(client);
        }

        public override void Run()
        {
            try
            {
                RunImpl();
            }
            catch (System.Exception ex)
            {
                _log.LogError(ex, $"error handling gs ({GetClient().IP}) message {this}");
            }
        }
    }
}
