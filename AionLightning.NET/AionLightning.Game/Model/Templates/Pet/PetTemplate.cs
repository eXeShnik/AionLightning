namespace AionLightning.Game.Model.Templates.Pet;

/// <summary>One &lt;pet&gt; row from pets/pets.xml (Java model.templates.pet.PetTemplate).</summary>
public sealed record PetTemplate(
    int Id,
    string Name,
    int NameId,
    int ConditionReward,
    List<PetFunction> Functions,
    PetStatsTemplate? Stats);
