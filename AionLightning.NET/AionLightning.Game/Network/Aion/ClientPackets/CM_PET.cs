using AionLightning.Commons.Network;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client issues a pet command (adopt/surrender/spawn/dismiss/rename/feed/mood). Opcode 0xF4.
/// Doping (action 9, actionType 2) and loot (actionType 3) are parsed but not yet acted on (P3).</summary>
public sealed class CM_PET : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly PetService _petService;

    private int _actionId;
    private int _petId;
    private int _decoration;
    private string _name = string.Empty;
    private int _actionType;
    private int _objectId;
    private int _count;
    private int _moodType;
    private int _emotionId;

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
            case 9: // FOOD
                _actionType = r.ReadD();
                if (_actionType == 3) // loot toggle
                    r.ReadD();
                else if (_actionType == 2) // P3: doping — parsed only, not acted on
                    { r.ReadD(); r.ReadD(); r.ReadD(); }
                else
                {
                    _objectId = r.ReadD();
                    _count = r.ReadD();
                    r.ReadD(); // unk2
                }
                break;
            case 10: // RENAME
                _petId = r.ReadD();
                _name = r.ReadS() ?? string.Empty;
                break;
            case 12: // MOOD
                _moodType = r.ReadD();
                _emotionId = r.ReadD();
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
            case 9:  await RunFeedAsync(player, ct); break;
            case 10: await _petService.RenameAsync(player, _conn, _petId, _name, ct); break;
            case 12: await RunMoodAsync(player, ct); break;
        }
    }

    private async ValueTask RunFeedAsync(Player player, CancellationToken ct)
    {
        if (_actionType is 2 or 3) return; // doping / loot toggle — P3
        var pet = player.ToyPet;
        if (pet is null) return;

        if (_objectId == 0)
        {
            pet.CommonData.CancelFeed = true;
            await _conn.SendAsync(SM_PET.Feed(4, 0, 0, pet.FeedProgress?.GetDataForPacket() ?? 0, 0), ct);
            await _conn.SendAsync(new SM_EMOTION(player, EmotionType.END_FEEDING, 0, player.ObjectId), ct);
        }
        else if (!pet.CommonData.IsFeedingTime)
        {
            int refeedSec = (int)(pet.CommonData.GetRefeedDelayMs() / 1000);
            await _conn.SendAsync(SM_PET.Feed(8, _objectId, _count, pet.FeedProgress?.GetDataForPacket() ?? 0, refeedSec), ct);
        }
        else
        {
            await _petService.FeedAsync(player, _conn, _objectId, _count, ct);
        }
    }

    private async ValueTask RunMoodAsync(Player player, CancellationToken ct)
    {
        var pet = player.ToyPet;
        if (pet is null) return;

        bool allowed = (_moodType == 0 && pet.CommonData.GetMoodRemainingTime() == 0)
            || (_moodType == 3 && pet.CommonData.GetGiftRemainingTime() == 0)
            || _emotionId != 0;
        if (allowed)
            await _petService.CheckMoodAsync(player, _conn, pet, _moodType, _emotionId, ct);
    }
}
