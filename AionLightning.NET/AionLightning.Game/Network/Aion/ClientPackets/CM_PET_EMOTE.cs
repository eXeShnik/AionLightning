using AionLightning.Commons.Network;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client drives its toy pet's movement/emote; the server relays it to nearby players. Opcode 0xF7.</summary>
public sealed class CM_PET_EMOTE : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly PetService _petService;

    private int _emoteId;
    private float _x, _y, _z, _x2, _y2, _z2;
    private byte _heading;
    private int _emotionId, _param1;

    public CM_PET_EMOTE(GsClientConnection conn, PetService petService)
    {
        _conn       = conn;
        _petService = petService;
    }

    public override void Read(ref PacketReader r)
    {
        _emoteId = r.ReadC();
        if (_emoteId == 0) // MOVE_STOP
        {
            _x = r.ReadF(); _y = r.ReadF(); _z = r.ReadF(); _heading = (byte)r.ReadC();
        }
        else if (_emoteId == 12) // MOVETO
        {
            _x = r.ReadF(); _y = r.ReadF(); _z = r.ReadF(); _heading = (byte)r.ReadC();
            _x2 = r.ReadF(); _y2 = r.ReadF(); _z2 = r.ReadF();
        }
        else
        {
            _emotionId = r.ReadC(); _param1 = r.ReadC();
        }
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        var pet = player?.ToyPet;
        if (player is null || pet is null) return;

        SM_PET_EMOTE packet;
        Position? newPos = null;
        switch (_emoteId)
        {
            case 0:
                packet = SM_PET_EMOTE.Stop(pet.ObjectId, _x, _y, _z, _heading);
                newPos = new Position(_x, _y, _z, _heading, player.Position.WorldId, player.Position.InstanceId);
                break;
            case 12:
                packet = SM_PET_EMOTE.Move(pet.ObjectId, _x, _y, _z, _heading, _x2, _y2, _z2);
                newPos = new Position(_x, _y, _z, _heading, player.Position.WorldId, player.Position.InstanceId);
                break;
            default:
                packet = SM_PET_EMOTE.Emotion(pet.ObjectId, _emoteId, _emotionId, _param1);
                break;
        }

        await _petService.RelayEmoteAsync(player, packet, newPos, ct);
    }
}
