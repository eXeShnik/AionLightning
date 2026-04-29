namespace AionLightning.Game.Configs.Options;

public sealed record RateOptions
{
    public float XpRate { get; init; } = 1.0f;
    public float PremiumXpRate { get; init; } = 2.0f;
    public float VipXpRate { get; init; } = 3.0f;
    public float DropRate { get; init; } = 1.0f;
    public float PremiumDropRate { get; init; } = 2.0f;
    public float VipDropRate { get; init; } = 3.0f;
    public float QuestXpRate { get; init; } = 2.0f;
    public float QuestKinahRate { get; init; } = 1.0f;
    public float GatheringXpRate { get; init; } = 1.0f;
    public float CraftingXpRate { get; init; } = 1.0f;
    public float ApPlayerGainRate { get; init; } = 1.0f;
    public double NormalMobsRateHp { get; init; } = 1.0;
    public double NormalMobsRatePw { get; init; } = 1.0;
    public double EliteMobsRateHp { get; init; } = 1.0;
    public double EliteMobsRatePw { get; init; } = 1.0;
}
