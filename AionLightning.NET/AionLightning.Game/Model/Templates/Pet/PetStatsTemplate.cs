namespace AionLightning.Game.Model.Templates.Pet;

/// <summary>&lt;petstats&gt; child element from pets.xml — movement/appearance stats for a pet template.
/// <see cref="Reaction"/> is a descriptive string ("brave", "cowardly", ...) per the Java template
/// (model.templates.pet.PetStatsTemplate), not a numeric id.</summary>
public sealed record PetStatsTemplate(string Reaction, float RunSpeed, float WalkSpeed, float Height, float Altitude);
