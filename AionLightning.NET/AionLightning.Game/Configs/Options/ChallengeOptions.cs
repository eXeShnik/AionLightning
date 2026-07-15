namespace AionLightning.Game.Configs.Options;

/// <summary>
/// ChallengeTaskService gate for the SM_CHALLENGE_LIST response — mirrors Java
/// CustomConfig.CHALLENGE_TASKS_ENABLED, which only ever guarded the client-facing task list display;
/// challenge/quest progress tracking and reward payout ran unconditionally in Java too (see
/// <see cref="Services.ChallengeTaskService.OnChallengeQuestFinishAsync"/>). Kept off by default until
/// SM_CHALLENGE_LIST's opcode/layout is confirmed against a live 4.6 client capture (see that packet's
/// own TODO).
/// </summary>
public sealed record ChallengeOptions
{
    public bool Enable { get; init; } = false;
}
