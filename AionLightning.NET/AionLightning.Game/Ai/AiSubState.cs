namespace AionLightning.Game.Ai;

/// <summary>Secondary AI state layered on top of <see cref="AiState"/> (Java <c>ai2.AISubState</c>).</summary>
public enum AiSubState
{
    None,
    Talk,
    Cast,
    WalkPath,
    WalkRandom,
    WalkWaitGroup,
    Freeze,
}
