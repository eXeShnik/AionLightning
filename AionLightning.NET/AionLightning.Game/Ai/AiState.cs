namespace AionLightning.Game.Ai;

/// <summary>Top-level AI lifecycle state (Java <c>ai2.AIState</c>).</summary>
public enum AiState
{
    Created,
    Died,
    Despawned,
    Idle,
    Walking,
    Following,
    Returning,
    Fight,
    Fear,
}
