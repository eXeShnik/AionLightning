using AionLightning.Commons.Network;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client issues a pet command (adopt/surrender/spawn/dismiss/rename). Opcode 0xF4.
/// Feed(9)/mood(12)/doping are parsed but not yet acted on (P2).</summary>
public sealed class CM_PET : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly PetService _petService;

    private int _actionId;
    private int _petId;
    private int _decoration;
    private string _name = string.Empty;

    public CM_PET(GsClientConnection conn, PetService petService)
    {
        _conn       = conn;
        _petService = petService;
    }

    public override void Read(ref PacketReader r)
    {
        _actionId = r.ReadH();
        switch (_actionId)
        {
            case 1: // ADOPT: eggObjId, petId, unk(C), unk(D), decoration, unk(D), unk(D), name
                r.ReadD();
                _petId = r.ReadD();
                r.ReadC();
                r.ReadD();
                _decoration = r.ReadD();
                r.ReadD(); r.ReadD();
                _name = r.ReadS() ?? string.Empty;
                break;
            case 2: // SURRENDER
            case 3: // SPAWN
            case 4: // DISMISS
                _petId = r.ReadD();
                break;
            case 9: // FOOD (P2)
                int actionType = r.ReadD();
                if (actionType == 3) r.ReadD();
                else { r.ReadD(); r.ReadD(); r.ReadD(); }
                break;
            case 10: // RENAME
                _petId = r.ReadD();
                _name = r.ReadS() ?? string.Empty;
                break;
            case 12: // MOOD (P2)
                r.ReadD(); r.ReadD();
                break;
        }
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        switch (_actionId)
        {
            case 1:  await _petService.AdoptAsync(player, _conn, _petId, _name, _decoration, ct); break;
            case 2:  await _petService.SurrenderAsync(player, _conn, _petId, ct); break;
            case 3:  await _petService.SpawnAsync(player, _conn, _petId, ct); break;
            case 4:  await _petService.DismissAsync(player, _conn, manual: true, ct); break;
            case 10: await _petService.RenameAsync(player, _conn, _petId, _name, ct); break;
            // 9 (food) / 12 (mood) are P2
        }
    }
}
