namespace AionLightning.Game.Model.Pet;

/// <summary>Feed-minigame satiety tier (Java services.toypet.PetHungryLevel). Cycles
/// Hungry → Content → SemiFull → Full → Hungry as the active pet is fed.</summary>
public enum PetHungryLevel : byte
{
    Hungry = 0,
    Content = 1,
    SemiFull = 2,
    Full = 3,
}

public static class PetHungryLevelExtensions
{
    public static PetHungryLevel GetNextValue(this PetHungryLevel level) => level switch
    {
        PetHungryLevel.Hungry   => PetHungryLevel.Content,
        PetHungryLevel.Content  => PetHungryLevel.SemiFull,
        PetHungryLevel.SemiFull => PetHungryLevel.Full,
        PetHungryLevel.Full     => PetHungryLevel.Hungry,
        _                       => PetHungryLevel.Hungry,
    };
}
