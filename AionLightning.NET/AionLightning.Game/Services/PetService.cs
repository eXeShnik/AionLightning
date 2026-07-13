using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Pet;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;
using PetModel = AionLightning.Game.Model.Pet.Pet;

namespace AionLightning.Game.Services;

/// <summary>
/// Toy-pet ownership + lifecycle (Java <c>services/toypet/PetService</c> + <c>PetAdoptionService</c>
/// + <c>PetSpawnService</c>, P1 subset). Handles adopt, list-on-login, spawn/dismiss of the active
/// companion pet, rename, surrender, and relaying client-driven pet movement to nearby players.
/// Feed/mood/doping/loot/warehouse minigames are P2 (persisted round-trip only, not computed here).
/// </summary>
public sealed class PetService
{
    private readonly IDataManager _dataManager;
    private readonly IPetDao _petDao;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly ILogger<PetService> _log;

    public PetService(IDataManager dataManager, IPetDao petDao,
        PlayerConnectionRegistry connRegistry, ILogger<PetService> log)
    {
        _dataManager  = dataManager;
        _petDao       = petDao;
        _connRegistry = connRegistry;
        _log          = log;
    }

    /// <summary>Loads a player's owned pets from the DB into <see cref="Player.Pets"/> (call on enter-world).</summary>
    public async Task LoadPetsAsync(Player player, CancellationToken ct = default)
    {
        var pets = await _petDao.LoadByPlayerIdAsync(player.ObjectId, ct);
        foreach (var pet in pets)
        {
            pet.PetObjectId = 0; // never spawned yet this session
            player.Pets.Add(pet);
        }
    }

    /// <summary>Sends the full owned-pet list (Java <c>PetService.onPlayerLogin</c> → SM_PET(0)).</summary>
    public async ValueTask SendListAsync(Player player, GsClientConnection conn, CancellationToken ct = default)
    {
        var list = new List<(PetCommonData, Model.Templates.Pet.PetTemplate)>();
        foreach (var data in player.Pets.All)
        {
            var tmpl = _dataManager.Pets.GetTemplate(data.PetId);
            if (tmpl is not null) list.Add((data, tmpl));
        }
        await conn.SendAsync(SM_PET.List(list), ct);
    }

    /// <summary>Adopts a pet (Java <c>PetAdoptionService.adoptPet</c>, P1). Egg-item consumption is P2.</summary>
    public async ValueTask AdoptAsync(Player player, GsClientConnection conn, int petId, string name, int decoration, CancellationToken ct = default)
    {
        var tmpl = _dataManager.Pets.GetTemplate(petId);
        if (tmpl is null || player.Pets.Has(petId)) return;

        var data = new PetCommonData
        {
            PetId          = petId,
            Name           = string.IsNullOrWhiteSpace(name) ? tmpl.Name : name,
            Decoration     = decoration,
            Birthday       = DateTime.UtcNow,
            MasterObjectId = player.ObjectId,
            Dopings        = "",
        };
        player.Pets.Add(data);
        await _petDao.InsertAsync(player.ObjectId, data, ct);
        await conn.SendAsync(SM_PET.Adopt(data, tmpl), ct);
    }

    /// <summary>Permanently gives up a pet (Java <c>PetAdoptionService.surrenderPet</c>).</summary>
    public async ValueTask SurrenderAsync(Player player, GsClientConnection conn, int petId, CancellationToken ct = default)
    {
        var data = player.Pets.Get(petId);
        if (data is null) return;

        if (player.ToyPet?.CommonData.PetId == petId)
            await DismissAsync(player, conn, manual: false, ct);

        player.Pets.Remove(petId);
        await _petDao.RemoveAsync(player.ObjectId, petId, ct);
        await conn.SendAsync(SM_PET.Surrender(data), ct);
    }

    /// <summary>Summons the pet into the world beside its master (Java <c>PetSpawnService.summonPet</c>).</summary>
    public async ValueTask SpawnAsync(Player player, GsClientConnection conn, int petId, CancellationToken ct = default)
    {
        var data = player.Pets.Get(petId);
        var tmpl = _dataManager.Pets.GetTemplate(petId);
        if (data is null || tmpl is null) return;

        if (player.ToyPet is not null)
            await DismissAsync(player, conn, manual: false, ct);

        var pet = new PetModel(player, tmpl, data)
        {
            ObjectId = ObjectIdFactory.Next(),
            Position = player.Position,
        };
        data.PetObjectId = pet.ObjectId;
        player.ToyPet = pet;
        player.Pets.LastUsedPetId = petId;

        await BroadcastToScopeAsync(player, SM_PET.Spawn(pet), ct);
    }

    /// <summary>Removes the active pet from the world (Java <c>PetSpawnService.dismissPet</c>).</summary>
    public async ValueTask DismissAsync(Player player, GsClientConnection conn, bool manual, CancellationToken ct = default)
    {
        var pet = player.ToyPet;
        if (pet is null) return;

        pet.CommonData.PetObjectId = 0;
        player.ToyPet = null;
        pet.Master = null;

        await BroadcastToScopeAsync(player, SM_PET.Dismiss(pet.ObjectId), ct);
    }

    /// <summary>Renames a pet (Java <c>PetService.renamePet</c>). Works whether or not the pet is spawned.</summary>
    public async ValueTask RenameAsync(Player player, GsClientConnection conn, int petId, string name, CancellationToken ct = default)
    {
        var data = player.Pets.Get(petId);
        if (data is null || string.IsNullOrWhiteSpace(name)) return;

        data.Name = name;
        if (player.ToyPet?.CommonData.PetId == petId)
            player.ToyPet.Name = name;

        await _petDao.UpdateNameAsync(player.ObjectId, petId, name, ct);
        await conn.SendAsync(SM_PET.Rename(data.PetObjectId, name), ct);
    }

    /// <summary>Relays a client-driven pet movement/emote to every player in the owner's scope.</summary>
    public async ValueTask RelayEmoteAsync(Player player, SM_PET_EMOTE packet, Position? newPetPosition, CancellationToken ct = default)
    {
        if (player.ToyPet is null) return;
        if (newPetPosition is { } np) player.ToyPet.Position = np;
        await BroadcastToScopeAsync(player, packet, ct);
    }

    /// <summary>Sends SM_PET(3) for every already-spawned pet in <paramref name="viewer"/>'s scope (Java visibility on zone-in).</summary>
    public async ValueTask SendVisiblePetsAsync(Player viewer, GsClientConnection viewerConn, CancellationToken ct = default)
    {
        foreach (var otherConn in _connRegistry.GetAllExcept(viewer.ObjectId))
        {
            var owner = otherConn.ActivePlayer;
            if (owner?.ToyPet is { } pet && owner.Position.SameScope(viewer.Position))
                try { await viewerConn.SendAsync(SM_PET.Spawn(pet), ct); } catch { }
        }
    }

    private async ValueTask BroadcastToScopeAsync(Player owner, AionServerPacket packet, CancellationToken ct)
    {
        var scope = owner.Position;
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer is { } p && p.Position.SameScope(scope))
                try { await conn.SendAsync(packet, ct); } catch { }
    }
}
