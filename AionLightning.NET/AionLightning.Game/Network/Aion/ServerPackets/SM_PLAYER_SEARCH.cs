using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

public sealed class SM_PLAYER_SEARCH : AionServerPacket
{
    private readonly IReadOnlyList<Player> _players;

    public SM_PLAYER_SEARCH(IEnumerable<Player> players) : base(0xD3)
        => _players = players.ToList();

    public override void Write(ref PacketWriter w)
    {
        w.WriteH((short)_players.Count);
        foreach (var p in _players)
        {
            w.WriteD(p.Position.WorldId);
            w.WriteF(p.Position.X);
            w.WriteF(p.Position.Y);
            w.WriteF(p.Position.Z);
            w.WriteC((byte)p.PlayerClass);
            w.WriteC((byte)p.Gender);
            w.WriteC(p.Level);
            w.WriteC(p.Group is not null ? (byte)3 : (byte)0); // 0=solo, 3=grouped
            w.WriteS(p.Name, 56);
        }
    }
}
