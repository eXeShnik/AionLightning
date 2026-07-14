using AionLightning.Commons.Network;
using AionLightning.Game.Model;
using AionLightning.Game.Model.House;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Java network.aion.serverpackets.SM_HOUSE_OWNER_INFO — sent on login (and studio purchase) to report
/// the player's active house (if any) plus their owner-status flags.
/// Opcode 0x107 (4.5-era packet table). TODO: verify opcode against a live 4.6 client capture before enabling.
/// </summary>
public sealed class SM_HOUSE_OWNER_INFO : AionServerPacket
{
    private readonly Player _player;
    private readonly House? _activeHouse;

    public SM_HOUSE_OWNER_INFO(Player player, House? activeHouse) : base(0x107)
    {
        _player = player;
        _activeHouse = activeHouse;
    }

    public override void Write(ref PacketWriter w)
    {
        if (_activeHouse is null)
        {
            w.WriteD(0);
            w.WriteD(_player.IsBuildingInState(PlayerHouseOwnerFlags.BuyStudioAllowed) ? 355000 : 0); // studio building id
        }
        else
        {
            w.WriteD(_activeHouse.Address);
            w.WriteD(_activeHouse.BuildingId);
        }
        w.WriteC(_player.BuildingOwnerState);

        // note: Java resolves the real town level via TownService (per address.getTownId()). TownService
        // isn't ported in this phase, so the town level is always the base value 1.
        w.WriteC(1);

        // note: Java computes weeks-until-maintenance-due via MaintenanceTask, which is P2+ (fee/maintenance
        // subsystem not ported). Always 0 (matches Java's "no fee owed / studio" branch) until that lands.
        w.WriteC(0);

        // Second house info — Java always writes zeroed placeholder fields here too.
        w.WriteD(0);
        w.WriteD(0);
        w.WriteD(0);
        w.WriteC(0);
        w.WriteC(0);
        w.WriteC(0);
        w.WriteC(0);
    }
}
