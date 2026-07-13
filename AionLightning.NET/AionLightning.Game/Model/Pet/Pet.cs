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

    public Pet(Player master, PetTemplate template, PetCommonData commonData)
    {
        Master = master;
        Template = template;
        CommonData = commonData;
        Name = commonData.Name;
        MovementSpeed = template.Stats?.RunSpeed ?? 6.0f;
    }
}
