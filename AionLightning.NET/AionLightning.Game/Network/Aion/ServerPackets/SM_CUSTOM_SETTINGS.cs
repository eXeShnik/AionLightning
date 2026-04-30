using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Broadcasts a player's display/deny social settings. Opcode 0xB8.</summary>
public sealed class SM_CUSTOM_SETTINGS : AionServerPacket
{
    private readonly int _objectId;
    private readonly int _displaySettings;
    private readonly int _denySettings;

    public SM_CUSTOM_SETTINGS(int objectId, int displaySettings, int denySettings) : base(0xB8)
    {
        _objectId        = objectId;
        _displaySettings = displaySettings;
        _denySettings    = denySettings;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_objectId);
        w.WriteC(0);
        w.WriteH(_displaySettings);
        w.WriteH(_denySettings);
    }
}
