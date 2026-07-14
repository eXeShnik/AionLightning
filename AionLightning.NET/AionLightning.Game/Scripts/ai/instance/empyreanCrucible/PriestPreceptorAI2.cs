// PriestPreceptorAI2 — Java ai/instance/empyreanCrucible/PriestPreceptorAI2.java. Empyrean Crucible
// add: opening self-buff plus HP-threshold escalation (75%/25%) that spawns three helper adds at 25%.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("priest_preceptor")]
public sealed class PriestPreceptorAI2 : AggressiveNpcAI2
{
    private bool _is75EventStarted;
    private bool _is25EventStarted;

    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(() => UseSkill(19612, 15), 1000);
    }

    public override void OnBackHome()
    {
        _is75EventStarted = false;
        _is25EventStarted = false;
        base.OnBackHome();
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckPercentage(getOwner().HpPercentage);
    }

    private void CheckPercentage(int percentage)
    {
        if (percentage <= 75 && !_is75EventStarted)
        {
            _is75EventStarted = true;
            // note: Java targeted a random living known player within 25m; known-list scanning isn't
            // exposed to scripts yet.
            UseSkill(19611, 10);
        }
        if (percentage <= 25 && !_is25EventStarted)
        {
            _is25EventStarted = true;
            StartEvent();
        }
    }

    private void StartEvent()
    {
        UseSkill(19610, 10);
        ScheduleTask(() =>
        {
            UseSkill(19614, 10);
            ScheduleTask(() =>
            {
                var p = getOwner().Position;
                ApplySoulSickness(Spawn(282366, p.X, p.Y, p.Z, (byte)p.Heading));
                ApplySoulSickness(Spawn(282367, p.X, p.Y, p.Z, (byte)p.Heading));
                ApplySoulSickness(Spawn(282368, p.X, p.Y, p.Z, (byte)p.Heading));
            }, 5000);
        }, 2000);
    }

    private void ApplySoulSickness(Npc? npc)
    {
        if (npc is null) return;
        ScheduleTask(() =>
        {
            npc.CurrentHp = npc.MaxHp / 2; // TODO: remove this, fix max hp debuffs not reducing current hp properly
            // note: Java also cast skill 19594 (level 4) onto this add; that cast targeted the add rather
            // than this AI's owner, which UseSkill doesn't model.
        }, 1000);
    }
}
