// InstanceTimerAI2 — Java ai/InstanceTimerAI2.java. Starts an instance countdown (broadcast via
// SM_QUEST_ACTION) the first time this NPC is attacked.
using System;
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("instancetimer")]
public sealed class InstanceTimerAI2 : AggressiveNpcAI2
{
    private static readonly HashSet<int> TimedNpcIds = new()
    {
        215222, 215221, 215179, 215178, 215136, 215135, 233719, 233676, 233633
    };

    private bool _isInTimer;
    private DateTime _timerStarted;
    private int _timerDurationMs;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (_isInTimer) return;
        _isInTimer = true;
        _timerStarted = DateTime.UtcNow;
        _timerDurationMs = TimedNpcIds.Contains(Owner.Template.NpcId) ? 600000 : 0;
        // note: Java broadcast the timer via SM_QUEST_ACTION to the attacker's team (or the attacker
        // directly when not teamed); team/player packet broadcast isn't wired at the script layer yet.
    }

    /// <summary>Elapsed milliseconds since the attack timer started (Java <c>getRemainigTime</c>; despite
    /// the name it reports elapsed time, not remaining), capped to 0 once past this instance's configured
    /// duration or when the timer hasn't started.</summary>
    public long GetRemainingTime()
    {
        if (!_isInTimer) return 0;
        long elapsed = (long)(DateTime.UtcNow - _timerStarted).TotalMilliseconds;
        return elapsed < 0 || elapsed > _timerDurationMs ? 0 : elapsed;
    }
}
