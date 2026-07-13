using AionLightning.Game.Model.Templates.Pet;

namespace AionLightning.Game.Model.Pet;

/// <summary>
/// A toy pet spawned into the world for its master (Java model.gameobjects.Pet). Mirrors
/// <see cref="Summon"/>'s integration with the Creature/world-visibility model, but carries no combat
/// state: pet movement is entirely client-driven, so unlike Summon there is no server-side move/AI
/// ticking here.
/// </summary>
public sealed class Pet : Creature
{
    public PetTemplate Template { get; }

    /// <summary>The player who owns this pet. Set to null once dismissal completes and the link is torn down.</summary>
    public Player? Master { get; set; }

    public PetCommonData CommonData { get; }

    /// <summary>Live feed-minigame state while spawned (Java's <see cref="PetCommonData"/> keeps this for
    /// the pet's whole lifetime in memory). Hydrated in <c>PetService.SpawnAsync</c> from the persisted
    /// packed columns and flushed back on <c>PetService.DismissAsync</c>; null for pets without a FOOD
    /// function. Kept off <see cref="PetCommonData"/> to keep persisted vs. session-only pet state separate.</summary>
    public PetFeedProgress? FeedProgress { get; set; }

    public Pet(Player master, PetTemplate template, PetCommonData commonData)
    {
        Master = master;
        Template = template;
        CommonData = commonData;
        Name = commonData.Name;
        MovementSpeed = template.Stats?.RunSpeed ?? 6.0f;
    }
}
