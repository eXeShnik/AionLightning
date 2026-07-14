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
    private readonly int _weeksUntilDue;
    private readonly int _townLevel;

    /// <param name="weeksUntilDue">Java's maintenance-weeks-left field, computed by the caller via
    /// <see cref="Services.MaintenanceTask.ComputeWeeksUntilDue"/> (0 for no house / unpaid / studio /
    /// P7 disabled — see that method and <see cref="Services.HousingService.OnPlayerLoginAsync"/>).</param>
    /// <param name="townLevel">Java resolves this via TownService.getTownById(address.getTownId()).getLevel()
    /// — callers pass <see cref="Services.TownService.GetTownLevel"/>'s result (default 1 for no house /
    /// unknown town, matching TownService's own fallback).</param>
    public SM_HOUSE_OWNER_INFO(Player player, House? activeHouse, int weeksUntilDue = 0, int townLevel = 1) : base(0x107)
    {
        _player = player;
        _activeHouse = activeHouse;
        _weeksUntilDue = weeksUntilDue;
        _townLevel = townLevel;
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
        w.WriteC((byte)_townLevel);

        // Weeks until the maintenance bill is due (0 if unpaid/no house/studio — client shows it in red).
        w.WriteC((byte)_weeksUntilDue);

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
