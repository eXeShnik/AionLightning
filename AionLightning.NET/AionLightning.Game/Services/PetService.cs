using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Model.Pet;
using AionLightning.Game.Model.Templates.Pet;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;
using PetModel = AionLightning.Game.Model.Pet.Pet;

namespace AionLightning.Game.Services;

/// <summary>
/// Toy-pet ownership + lifecycle (Java <c>services/toypet/PetService</c> + <c>PetAdoptionService</c>
/// + <c>PetSpawnService</c> + <c>PetMoodService</c>). P1 covers adopt, list-on-login, spawn/dismiss of
/// the active companion pet, rename, surrender, and relaying client-driven pet movement to nearby
/// players. P2 adds the feed (<see cref="FeedAsync"/>) and mood (<see cref="CheckMoodAsync"/>) minigames.
/// Doping/loot/warehouse remain P3.
/// </summary>
public sealed class PetService
{
    private readonly IDataManager _dataManager;
    private readonly IPetDao _petDao;
    private readonly IItemDao _itemDao;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly ILogger<PetService> _log;

    public PetService(IDataManager dataManager, IPetDao petDao, IItemDao itemDao,
        PlayerConnectionRegistry connRegistry, ILogger<PetService> log)
    {
        _dataManager  = dataManager;
        _petDao       = petDao;
        _itemDao      = itemDao;
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

        var foodFunc = tmpl.Functions.FirstOrDefault(f => string.Equals(f.Type, "FOOD", StringComparison.OrdinalIgnoreCase));
        var flavour = foodFunc is not null ? _dataManager.PetFeed.GetFlavourById(foodFunc.Id) : null;
        if (flavour is not null)
            pet.FeedProgress = data.HydrateFeedProgress(flavour.LovedFoodLimit);

        await BroadcastToScopeAsync(player, SM_PET.Spawn(pet), ct);
    }

    /// <summary>Removes the active pet from the world (Java <c>PetSpawnService.dismissPet</c>) — flushes
    /// the live feed/mood minigame state back to the persisted <c>player_pets</c> columns.</summary>
    public async ValueTask DismissAsync(Player player, GsClientConnection conn, bool manual, CancellationToken ct = default)
    {
        var pet = player.ToyPet;
        if (pet is null) return;

        var data = pet.CommonData;
        if (pet.FeedProgress is not null)
        {
            data.CancelFeed = true;
            data.PersistFeedProgress(pet.FeedProgress);
            await _petDao.SaveFeedStatusAsync(player.ObjectId, data.PetId, data.HungryLevel, data.FeedProgress, data.ReuseTime, ct);
        }

        if (manual) data.DespawnTime = DateTime.UtcNow;
        await _petDao.SaveMoodDataAsync(player.ObjectId, data.PetId, data.MoodStarted, data.Counter,
            data.MoodCdStarted, data.GiftCdStarted, data.DespawnTime, ct);

        data.PetObjectId = 0;
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

    /// <summary>
    /// Feeds the active pet with a stacked item (Java <c>PetService.removeObject</c> → scheduled
    /// <c>checkFeeding</c>, P2). Sends the "eat" ack immediately, then — like Java's 2500ms
    /// <c>ThreadPoolManager</c> schedule — resolves the actual feed result on a delayed background step
    /// (see <see cref="ScheduleCheckFeeding"/>) so this call returns without holding up the packet loop.
    /// </summary>
    public async ValueTask FeedAsync(Player player, GsClientConnection conn, long itemObjectId, int count, CancellationToken ct = default)
    {
        var pet = player.ToyPet;
        var item = player.Inventory.Get(itemObjectId);
        if (pet is null || pet.FeedProgress is null || item is null || count > item.Count) return;

        pet.CommonData.CancelFeed = false;
        await conn.SendAsync(SM_PET.Feed(1, itemObjectId, count, pet.FeedProgress.GetDataForPacket(), 0), ct);
        await conn.SendAsync(new SM_EMOTION(player, EmotionType.START_FEEDING, 0, player.ObjectId), ct);

        ScheduleCheckFeeding(player, conn, pet, itemObjectId, count, 2500);
    }

    private void ScheduleCheckFeeding(Player player, GsClientConnection conn, PetModel pet, long itemObjectId, int count, int delayMs)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(delayMs);
            if (!pet.CommonData.CancelFeed)
                await CheckFeedingAsync(player, conn, pet, itemObjectId, count);
        });
    }

    private async ValueTask CheckFeedingAsync(Player player, GsClientConnection conn, PetModel pet, long itemObjectId, int count)
    {
        var commonData = pet.CommonData;
        var progress = pet.FeedProgress;
        if (commonData.CancelFeed || progress is null) return;

        var item = player.Inventory.Get(itemObjectId);
        if (item is null) return;

        var foodFunc = pet.Template.Functions.FirstOrDefault(f => string.Equals(f.Type, "FOOD", StringComparison.OrdinalIgnoreCase));
        var flavour = foodFunc is not null ? _dataManager.PetFeed.GetFlavourById(foodFunc.Id) : null;
        if (flavour is null) return;

        var itemTemplate = _dataManager.Items.GetTemplate(item.ItemId);
        var rewardGroup = _dataManager.PetFeed.ResolveFoodGroup(flavour, item.ItemId);
        if (rewardGroup is { Loved: true } && progress.LovedFoodRemaining == 0)
            rewardGroup = null; // loved-food slots exhausted — treat as non-eatable for this flavour

        PetFeedResult? reward = null;

        if (rewardGroup is null)
        {
            // non-eatable item
            await conn.SendAsync(SM_PET.Feed(5, 0, 0, progress.GetDataForPacket(), (int)(commonData.GetRefeedDelayMs() / 1000)));
            await conn.SendAsync(new SM_EMOTION(player, EmotionType.END_FEEDING, 0, player.ObjectId));
            await conn.SendAsync(SM_SYSTEM_MESSAGE.ToypetFeedNotLoveFlavor(pet.Name, itemTemplate?.Name ?? string.Empty));
            return;
        }

        await ConsumeOneAsync(player, conn, item);

        int maxFeedCount = 1;
        if (rewardGroup.Loved) progress.SetIsLovedFeeded();
        else maxFeedCount = flavour.FullCount;

        PetFeedCalculator.EnsureInitialized(_dataManager.PetFeed);
        PetFeedCalculator.UpdatePetFeedProgress(progress, itemTemplate?.Level ?? 1, maxFeedCount);
        if (progress.HungryLevel == PetHungryLevel.Full)
            reward = PetFeedCalculator.GetReward(maxFeedCount, rewardGroup, progress, player.Level, _dataManager.Items);

        if (progress.HungryLevel == PetHungryLevel.Full && reward is not null)
            await conn.SendAsync(SM_PET.Feed(2, itemObjectId, 0, progress.GetDataForPacket(), 0));
        else
        {
            count--;
            await conn.SendAsync(SM_PET.Feed(2, itemObjectId, count, progress.GetDataForPacket(), 0));
        }

        if (progress.HungryLevel == PetHungryLevel.Full && reward is not null)
        {
            await conn.SendAsync(SM_PET.Feed(6, reward.Item, 0, progress.GetDataForPacket(), 0));
            await conn.SendAsync(SM_PET.Feed(5, 0, 0, progress.GetDataForPacket(), (int)(commonData.GetRefeedDelayMs() / 1000)));
            await conn.SendAsync(new SM_EMOTION(player, EmotionType.END_FEEDING, 0, player.ObjectId));
            await conn.SendAsync(SM_PET.Feed(7, reward.Item, 0, progress.GetDataForPacket(), (int)(commonData.GetRefeedDelayMs() / 1000)));

            await GrantItemAsync(player, conn, reward.Item, 1);

            long refeedMs = flavour.CooldownMinutes * 60_000L;
            long refeedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + refeedMs;
            commonData.IsFeedingTime = false;
            commonData.ReuseTime = refeedAt;
            await _petDao.SetRefeedTimeAsync(player.ObjectId, commonData.PetId, refeedAt);
            progress.Reset();

            _ = Task.Run(async () =>
            {
                await Task.Delay((int)refeedMs);
                commonData.IsFeedingTime = true;
                commonData.ReuseTime = 0;
                progress.HungryLevel = PetHungryLevel.Hungry;
            });
        }
        else if (count > 0)
        {
            ScheduleCheckFeeding(player, conn, pet, itemObjectId, count, 2500);
        }
        else
        {
            await conn.SendAsync(SM_PET.Feed(5, 0, 0, progress.GetDataForPacket(), (int)(commonData.GetRefeedDelayMs() / 1000)));
            await conn.SendAsync(new SM_EMOTION(player, EmotionType.END_FEEDING, 0, player.ObjectId));
        }
    }

    private async ValueTask ConsumeOneAsync(Player player, GsClientConnection conn, Item item)
    {
        item.Count -= 1;
        if (item.Count <= 0)
        {
            player.Inventory.Remove(item.UniqueId);
            await _itemDao.DeleteAsync(item.UniqueId, CancellationToken.None);
            await conn.SendAsync(new SM_DELETE_ITEM(item.UniqueId));
        }
        else
        {
            await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, CancellationToken.None);
            await conn.SendAsync(new SM_INVENTORY_ADD_ITEM([item]));
        }
    }

    /// <summary>
    /// Pet-affection minigame (Java <c>services/toypet/PetMoodService.checkMood</c>, P2). type 0 = client
    /// polls current mood status; 1 = player interacts with the pet (shuggle); 3 = requests the
    /// mood-fill gift.
    /// </summary>
    public async ValueTask CheckMoodAsync(Player player, GsClientConnection conn, PetModel pet, int type, int shuggleEmotion, CancellationToken ct = default)
    {
        switch (type)
        {
            case 0: await StartCheckingMoodAsync(conn, pet, ct); break;
            case 1: await InteractWithPetAsync(conn, pet, shuggleEmotion, ct); break;
            case 3: await RequestPresentAsync(player, conn, pet, ct); break;
        }
    }

    private static async ValueTask StartCheckingMoodAsync(GsClientConnection conn, PetModel pet, CancellationToken ct)
    {
        var data = pet.CommonData;
        int points = data.GetMoodPoints(forPacket: true);
        // Java quirk: LastSentPoints is only refreshed on the no-delta branch, so a repeated poll
        // right after a nonzero delta would resend the same delta. Preserved for protocol fidelity.
        int delta = data.LastSentPoints < points ? points - data.LastSentPoints : 0;
        if (delta == 0) data.LastSentPoints = points;
        await conn.SendAsync(SM_PET.MoodStatus(delta), ct);
    }

    private static async ValueTask InteractWithPetAsync(GsClientConnection conn, PetModel pet, int shuggleEmotion, CancellationToken ct)
    {
        var data = pet.CommonData;
        if (!data.IncreaseShuggleCounter()) return;

        int points = data.GetMoodPoints(forPacket: true);
        data.LastSentPoints = points;
        await conn.SendAsync(SM_PET.MoodEmotion(points, shuggleEmotion), ct);
        await conn.SendAsync(SM_PET.MoodPeriodic(points, data.GetMoodRemainingTime(), data.GetGiftRemainingTime()), ct); // update progress immediately
    }

    private async ValueTask RequestPresentAsync(Player player, GsClientConnection conn, PetModel pet, CancellationToken ct)
    {
        var data = pet.CommonData;
        if (data.GetMoodPoints(forPacket: false) < 9000)
        {
            _log.LogWarning("Requested pet present before mood fill up: {Player}", player.Name);
            return;
        }
        if (data.GetGiftRemainingTime() > 0)
        {
            _log.LogWarning("Player {Player} tried to get a pet gift during cooldown for pet {PetId}", player.Name, data.PetId);
            return;
        }
        if (!player.Inventory.HasFreeSlot)
        {
            await conn.SendAsync(SM_SYSTEM_MESSAGE.PetGiftInventoryFull(), ct);
            return;
        }

        data.ClearMoodStatistics();
        await conn.SendAsync(SM_PET.MoodPeriodic(data.GetMoodPoints(true), data.GetMoodRemainingTime(), data.GetGiftRemainingTime()), ct);
        data.GiftCdStarted = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await conn.SendAsync(SM_PET.MoodGift(pet.Template.ConditionReward), ct);

        if (pet.Template.ConditionReward != 0)
            await GrantItemAsync(player, conn, pet.Template.ConditionReward, 1, ct);
    }

    private async ValueTask GrantItemAsync(Player player, GsClientConnection conn, int itemId, long count, CancellationToken ct = default)
    {
        int maxStack = _dataManager.Items.GetTemplate(itemId)?.MaxStackCount ?? 1;
        if (!player.Inventory.CanReceive(itemId, maxStack)) return;

        var existing = player.Inventory.FindByItemId(itemId);
        Item granted;
        if (existing is not null)
        {
            existing.Count += count;
            granted = existing;
        }
        else
        {
            long uid = await _itemDao.NextUniqueIdAsync(ct);
            granted = new Item { UniqueId = uid, ItemId = itemId, Count = count, Slot = -1 };
            player.Inventory.Add(granted);
        }
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await conn.SendAsync(new SM_INVENTORY_ADD_ITEM([granted]), ct);
    }

    private async ValueTask BroadcastToScopeAsync(Player owner, AionServerPacket packet, CancellationToken ct)
    {
        var scope = owner.Position;
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer is { } p && p.Position.SameScope(scope))
                try { await conn.SendAsync(packet, ct); } catch { }
    }
}
