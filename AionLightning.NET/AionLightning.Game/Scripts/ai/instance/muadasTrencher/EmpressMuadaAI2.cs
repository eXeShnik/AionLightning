using System;
// EmpressMuadaAI2 — Java ai/instance/muadasTrencher/EmpressMuadaAI2.java. Muada's Trencher end
// boss: gated multi-phase encounter (helper waves at 75%/50%, forced disengage + add spawn at 25%).
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("empress_muada")]
public sealed class EmpressMuadaAI2 : AggressiveNpcAI2
{
    private bool _isAggred;
    private bool _phaseStarted;
    private bool _sandSquallStarted;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (!_isAggred)
        {
            _isAggred = true;
            StartEnrageTask();
        }
        CheckPercentage(Owner.HpPercentage);
    }

    private void CheckPercentage(int hpPercentage)
    {
        if (hpPercentage <= 75 && !_phaseStarted)
        {
            _phaseStarted = true;
            StartPhaseTask();
        }
        if (hpPercentage <= 50 && _phaseStarted)
        {
            _phaseStarted = false;
            // note: Java also despawned the 282556/282535/282539 helper NPCs here (WorldMapInstance
            // .getNpcs + Npc.getController().onDelete); per-instance NPC enumeration/deletion isn't
            // exposed to the script layer, so the helpers are left in place.
        }
        if (hpPercentage <= 25 && !_sandSquallStarted)
        {
            _sandSquallStarted = true;
            UseSkill(20499, 2);
            ScheduleTask(() =>
            {
                if (!Owner.IsAlreadyDead)
                {
                    UseSkill(19893, 2);
                    Spawn(282533, 523.1f, 541.1f, 106.7f);
                    SendMsg(1401300);
                    // note: Java re-enabled canThink() (which has no C# equivalent) after 20s, then
                    // either re-engaged the AggroList's most-hated target (MoveController/GameStats
                    // renewal + a skill cast at that target) or fell back to AIState.FIGHT; aggro/
                    // move-controller state isn't exposed to the script layer.
                }
            }, 3500);
        }
    }

    private void StartEnrageTask()
    {
        ScheduleTask(() =>
        {
            if (!Owner.IsAlreadyDead)
            {
                UseSkill(19859, 2);
                ScheduleTask(() =>
                {
                    if (!Owner.IsAlreadyDead) UseSkill(20089, 2);
                }, 15000);
            }
        }, 1_200_000);
    }

    private void StartPhaseTask()
    {
        ScheduleTask(() =>
        {
            if (Owner.IsAlreadyDead) return;
            UseSkill(19898, 2);
            for (int i = 0; i < 3; i++)
            {
                SendMsg(1401299);
                // note: Java spawned a 282556 anchor plus two 282535 helper NPCs at a random point
                // around the owner and shouted 1500307 from each helper (NpcShoutsService with an
                // explicit target); shouting from a non-owner NPC isn't exposed via the SendMsg helper,
                // so only the owner's own shout above is emitted.
                float direction = Random.Shared.Next(0, 200) / 100f;
                int distance = Random.Shared.Next(2, 6);
                float x = Owner.Position.X + MathF.Cos(MathF.PI * direction) * distance;
                float y = Owner.Position.Y + MathF.Sin(MathF.PI * direction) * distance;
                Spawn(282556, x, y, Owner.Position.Z);
                Spawn(282535, x, y, Owner.Position.Z);
                Spawn(282535, x, y, Owner.Position.Z);
            }
        }, 1000, 45000);
    }

    public override void OnBackHome()
    {
        // note: Java also despawned the 282556/282535/282539 helper NPCs here (WorldMapInstance
        // lookups); not exposed to the script layer.
        Owner.RemoveEffectBySkillId(19859);
        _isAggred = false;
        _phaseStarted = false;
        _sandSquallStarted = false;
        base.OnBackHome();
    }

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java also despawned the 282556/282535/282539 helper NPCs on death; per-instance NPC
        // enumeration/deletion isn't exposed to the script layer.
    }
}
