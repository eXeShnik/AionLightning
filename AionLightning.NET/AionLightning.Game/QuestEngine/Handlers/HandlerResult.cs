namespace AionLightning.Game.QuestEngine.Handlers;

/// <summary>Tri-state result of a bonus-apply quest hook (Java <c>questEngine.handlers.HandlerResult</c>).</summary>
public enum HandlerResult
{
    Unknown,
    Success,
    Failed,
}
