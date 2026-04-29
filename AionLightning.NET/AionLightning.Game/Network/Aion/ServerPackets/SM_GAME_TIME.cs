using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends current game time (minutes elapsed since 2000-01-01 00:00 UTC).</summary>
public sealed class SM_GAME_TIME : AionServerPacket
{
    public SM_GAME_TIME() : base(0x26) { }

    public override void Write(ref PacketWriter w)
    {
        var epoch = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        int minutes = (int)(DateTime.UtcNow - epoch).TotalMinutes;
        w.WriteD(minutes);
    }
}
