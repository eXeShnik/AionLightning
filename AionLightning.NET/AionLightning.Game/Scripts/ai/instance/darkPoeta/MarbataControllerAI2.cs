// MarbataControllerAI2 — Java ai/instance/darkPoeta/MarbataControllerAI2.java. Aura-controller npc
// that toggles a buff/debuff pair on its matching boss instance while alive.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("marbatacontroller")]
public sealed class MarbataControllerAI2 : NpcAi2
{
    private Npc? GetBoss() => Owner.Template.NpcId switch
    {
        700443 or 700444 or 700442 => GetNpc(214850),
        700446 or 700447 or 700445 => GetNpc(214851),
        700440 or 700441 or 700439 => GetNpc(214849),
        _ => null
    };

    private void ApplyEffect(bool remove)
    {
        var boss = GetBoss();
        if (boss is null || boss.IsAlreadyDead) return;

        switch (Owner.Template.NpcId)
        {
            case 700443:
            case 700446:
            case 700440:
                if (remove)
                    boss.RemoveEffectBySkillId(18556);
                else
                    // note: Java cast skill 18556 through the BOSS's own controller (boss.getController()
                    // .useSkill); commanding another NPC's skill cast isn't wired at the script layer yet.
                    _ = boss;
                break;
            case 700444:
            case 700447:
            case 700441:
                // note: Java left this branch as a TODO (unimplemented).
                break;
            case 700442:
            case 700445:
            case 700439:
                if (remove)
                    boss.RemoveEffectBySkillId(18110);
                else
                    // note: Java cast skill 18110 through the BOSS's own controller (boss.getController()
                    // .useSkill); commanding another NPC's skill cast isn't wired at the script layer yet.
                    _ = boss;
                break;
        }
    }

    public override void OnDied()
    {
        base.OnDied();
        ApplyEffect(true);
        // note: Java self-deleted via AI2Actions.deleteOwner(this); self-delete isn't wired at the
        // script layer yet.
    }

    public override void OnSpawned()
    {
        base.OnSpawned();
        ApplyEffect(false);
        ScheduleTask(UseControllerSkill, 2000);
    }

    private void UseControllerSkill()
    {
        if (Owner.IsAlreadyDead) return;
        // note: Java targeted itself (AI2Actions.targetSelf) before casting; self-targeting isn't
        // wired at the script layer yet.
        int skill = Owner.Template.NpcId switch
        {
            700443 or 700446 or 700440 => 18554,
            700444 or 700447 or 700441 => 18555,
            700442 or 700445 or 700439 => 18553,
            _ => 0
        };
        if (skill != 0) UseSkill(skill);
    }

    // note: Java's pollInstance(AIQuestion) override (SHOULD_DECAY/SHOULD_RESPAWN/SHOULD_REWARD all
    // NEGATIVE) has no NpcAi2 equivalent — dropped.
}
