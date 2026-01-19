using System.Buffers;
using AionLightning.Commons.Network.Packet;
using AionLightning.Login.Network.Aion;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login.Network.Aion;

public abstract class AionClientPacket : BaseClientPacket<LoginConnection>
{
    protected readonly ILogger _logger;

    protected AionClientPacket(ILogger logger, ReadOnlySequence<byte> buffer, LoginConnection client) : base(buffer)
    {
        _logger = logger;
        Connection = client;
    }

    public override void Run()
    {
        try
        {
            RunImpl();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error handling client packet");
        }
    }

    protected abstract void RunImpl();
}