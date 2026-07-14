// BrigadeGeneralTerathAI2 — Java ai/instance/tiamatStrongHold/BrigadeGeneralTerathAI2.java.
// Tiamat Stronghold boss: periodic gravity-distortion adds plus a one-time HP-breakpoint "gravity
// event" that walks the boss home and suppresses thinking for a while.
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("brigadegeneralterath")]
public sealed class BrigadeGeneralTerathAI2 : AggressiveNpcAI2
{
    private bool _isHome = true;
    private readonly List<int> _percents = new();
    private Npc? _aethericField;
    private bool _isGravityEvent;
    private bool _isFinalBuff;

    // note: Java's canThink() gated the ai2 think loop on this flag while the gravity event ran —
    // NpcAi2 has no equivalent gate hook, so it is tracked but unused by the tick loop.
    private bool _canThink = true;

    public override void OnAttack(Creature attacker)
    {
        base.OnAttack(attacker);
        if (_aethericField is null)
        {
            _aethericField = Spawn(730692, 1030.08f, 1030.08f, 1030.08f);
            // note: Java closed instance door 706 here — instance doors aren't exposed to scripts yet.
        }
        if (_isHome && !_isGravityEvent)
        {
            _isHome = false;
            StartSkillTask();
        }
        CheckPercentage(Owner.HpPercentage);
        if (!_isFinalBuff && Owner.HpPercentage <= 25)
        {
            _isFinalBuff = true;
            UseSkill(20942);
        }
    }

    private void StartSkillTask()
    {
        ScheduleTask(() =>
        {
            if (Owner.IsAlreadyDead) CancelTasks();
            else GravityDistortionEvent();
        }, 5000, 30000);
    }

    private void GravityDistortionEvent()
    {
        UseSkill(20739);
        Spawn(283096, Owner.Position.X, Owner.Position.Y, Owner.Position.Z);
        Spawn(283097, Owner.Position.X, Owner.Position.Y, Owner.Position.Z);
        Spawn(283098, Owner.Position.X, Owner.Position.Y, Owner.Position.Z);
        ScheduleTask(() => UseSkill(20741), 5000);
    }

    private void CheckPercentage(int hpPercentage)
    {
        if (_percents.Count == 0) return;
        int percent = _percents[0];
        if (hpPercentage <= percent && !_isGravityEvent)
        {
            _percents.Remove(percent);
            _canThink = false;
            _isGravityEvent = true;
            CancelTasks();
            Spawn(283558, 1056.8f, 297.6f, 409.9f);
            Spawn(283558, 1002.07f, 297.4f, 409.85f);
            UseSkill(20737);
            ScheduleTask(() =>
            {
                // note: Java stopped the attack emote, walked the boss back to its spawn point via
                // EmoteManager/WalkManager/MoveController, and broadcast an SM_EMOTION — none of that
                // movement/emote plumbing is exposed to scripts yet.
            }, 4000);
            ScheduleTask(() =>
            {
                Spawn(283110, 1029.9f, 297.26f, 409.08f);
                Spawn(283109, 1029.93f, 297.31f, 409.08f);
            }, 10000);
            ScheduleTask(() =>
            {
                // note: Java bulk-deleted the spawned 283558/283110/283109 adds here (no scripted
                // NPC-removal API exists yet) and either resumed thinking toward the current
                // most-hated attacker or, if none remained in range, re-entered the FIGHT state —
                // aggro-list/move-controller access isn't exposed to scripts yet.
                Owner.RemoveEffectBySkillId(20737);
                _canThink = true;
                _isGravityEvent = false;
                StartSkillTask();
            }, 30000);
        }
    }

    public override void OnDied()
    {
        base.OnDied();
        _percents.Clear();
        CancelTasks();
        // note: Java deleted the aetheric field NPC and reopened instance door 706 here — NPC
        // removal and instance doors aren't exposed to scripts yet.
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        AddPercent();
        _isFinalBuff = false;
        CancelTasks();
        _isGravityEvent = false;
        _canThink = true;
        _isHome = true;
        // note: Java deleted the aetheric field NPC and reopened instance door 706 here — NPC
        // removal and instance doors aren't exposed to scripts yet.
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
        CancelTasks();
    }

    public override void OnSpawned()
    {
        base.OnSpawned();
        AddPercent();
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 90, 70, 50, 30, 25 });
    }
}
