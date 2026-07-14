namespace AionLightning.Game.Ai;

/// <summary>
/// AI event kinds dispatched into an <see cref="NpcAi2"/> (Java <c>ai2.event.AIEventType</c>). Not
/// every event is wired to a dispatcher yet in this port — the enum exists so scripts and future
/// event-plumbing code share the same vocabulary as the Java original.
/// </summary>
public enum AiEventType
{
    Activate,
    Deactivate,
    Freeze,
    Unfreeze,

    /// <summary>Creature is being attacked (internal).</summary>
    Attack,
    /// <summary>Creature's attack part is complete (internal).</summary>
    AttackComplete,
    /// <summary>Creature is stopping its attack (internal).</summary>
    AttackFinish,
    /// <summary>Some neighbour creature is being attacked (broadcast).</summary>
    CreatureNeedsSupport,
    MoveValidate,
    MoveArrived,
    CreatureSee,
    CreatureNotSee,
    CreatureMoved,
    CreatureAggro,
    Spawned,
    Respawned,
    Despawned,
    Died,
    TargetReached,
    TargetTooFar,
    TargetGiveup,
    TargetChanged,
    FollowMe,
    StopFollowMe,
    NotAtHome,
    BackHome,
    DialogStart,
    DialogFinish,
    DropRegistered,
}
