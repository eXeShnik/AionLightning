using AionLightning.Commons.Network;
using AionLightning.Game.Model.Town;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Java network.aion.serverpackets.SM_TOWNS_LIST — sends every town's level for the player's own race.
/// Opcode 0xE2 (4.5-era packet table). TODO: verify opcode against a live 4.6 client capture before
/// enabling by default — see <see cref="Configs.Options.TownOptions.SendTownListOnLogin"/>.
/// </summary>
public sealed class SM_TOWNS_LIST : AionServerPacket
{
    private readonly IReadOnlyCollection<Town> _towns;

    public SM_TOWNS_LIST(IReadOnlyCollection<Town> towns) : base(0xE2) => _towns = towns;

    public override void Write(ref PacketWriter w)
    {
        w.WriteH(_towns.Count);
        foreach (var town in _towns)
        {
            w.WriteD(town.Id);
            w.WriteD(town.Level);
            w.WriteD((int)new DateTimeOffset(town.LevelUpDate).ToUnixTimeSeconds());
        }
    }
}
