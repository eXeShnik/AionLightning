namespace AionLightning.Game.Model.Templates.Pet;

/// <summary>One &lt;petfunction&gt; child element from pets.xml — grants the pet a capability
/// (FOOD, WAREHOUSE, BAG, WING, LOOT, DOPING). <see cref="Id"/> is function-specific (e.g. a flavour
/// id for FOOD, a bag template id for BAG); <see cref="Slots"/> is only meaningful for WAREHOUSE.</summary>
public sealed record PetFunction(string Type, int Id, int Slots);
