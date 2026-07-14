using AionLightning.Commons.Network;
using AionLightning.Game.Model.Siege;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Java network.aion.serverpackets.SM_ABYSS_ARTIFACT_INFO.
/// Opcode 0xDC (4.5-era packet table). TODO: verify opcode against a live 4.6 client capture before enabling.
/// </summary>
public sealed class SM_ABYSS_ARTIFACT_INFO : AionServerPacket
{
    private readonly IReadOnlyCollection<ArtifactLocation> _locations;

    public SM_ABYSS_ARTIFACT_INFO(IReadOnlyCollection<ArtifactLocation> locations) : base(0xDC)
    {
        _locations = locations;
    }

    public SM_ABYSS_ARTIFACT_INFO(int locationId, SiegeService siegeService) : base(0xDC)
    {
        var artifact = siegeService.GetArtifact(locationId);
        _locations = artifact is null ? [] : [artifact];
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteH((short)_locations.Count);
        foreach (var artifact in _locations)
        {
            w.WriteD(artifact.LocationId * 10 + 1);
            w.WriteC((byte)artifact.Status);
            w.WriteD(0);
        }
    }
}
