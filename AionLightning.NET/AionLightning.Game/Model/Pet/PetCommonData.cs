namespace AionLightning.Game.Model.Pet;

/// <summary>
/// Persisted + session state for one adopted toy pet (Java model.gameobjects.player.PetCommonData).
/// One instance per player_pets row; lives in the owning Player's <see cref="PetList"/> for the whole
/// session, independent of whether the pet is currently spawned in the world.
/// </summary>
public sealed class PetCommonData
{
    public int PetId { get; set; }
    public int Decoration { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Birthday { get; set; }
    public int MasterObjectId { get; set; }

    /// <summary>World object id assigned via ObjectIdFactory.Next() when the pet is spawned; 0 while despawned.</summary>
    public int PetObjectId { get; set; }

    /// <summary>Absolute time this pet's adoption expires (rental/event eggs); null = never expires.</summary>
    public DateTime? ExpireTime { get; set; }

    /// <summary>Last time the pet was despawned/parked; PetList uses this to pick the "last used" pet on login.</summary>
    public DateTime? DespawnTime { get; set; }

    // Feed/mood/doping raw columns — round-tripped for persistence only; P1 does not compute them.
    public int HungryLevel { get; set; }
    public int FeedProgress { get; set; }
    public long ReuseTime { get; set; }
    public string Dopings { get; set; } = string.Empty;
    public long MoodStarted { get; set; }
    public int Counter { get; set; }
    public long MoodCdStarted { get; set; }
    public long GiftCdStarted { get; set; }
}
