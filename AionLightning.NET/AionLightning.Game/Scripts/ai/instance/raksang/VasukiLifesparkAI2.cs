// VasukiLifesparkAI2 — Java ai/instance/raksang/VasukiLifesparkAI2.java. One of a set of lifespark
// props: casts a per-npc-id skill sequence once a player wanders close, then self-removes.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("vasuki_lifespark")]
public sealed class VasukiLifesparkAI2 : AggressiveNpcAI2
{
    private bool _startedEvent;

    // note: Java gated its own think loop through canThink() — always false, except for npc 217764
    // (true); no C# equivalent hook exists.

    public override void OnSpawned()
    {
        if (getOwner().Template.NpcId != 217764)
        {
            ScheduleTask(() =>
            {
                if (!getOwner().IsAlreadyDead)
                    UseSkill(19126, 46);
            }, 3000);
        }
        base.OnSpawned();
    }

    public override void OnCreatureMoved(Creature creature)
    {
        if (creature is not Player player) return;
        if (getOwner().Position.DistanceTo(player.Position) > 30) return;
        if (_startedEvent) return;
        _startedEvent = true;

        int npcId = getOwner().Template.NpcId;
        int level;
        int skill;
        switch (npcId)
        {
            case 217760:
                skill = 19972;
                level = 45;
                break;
            case 217761:
                skill = 19972;
                level = 46;
                break;
            case 217763:
                skill = 20087;
                level = 46;
                break;
            default:
                skill = 20039;
                level = 46;
                break;
        }
        // note: Java shouted a per-npc-id message (1401107/1401171/1401110, or none for 217763) via
        // NpcShoutsService here; NPC shouts aren't exposed to scripts yet.
        UseSkill(skill, level);
        if (npcId != 217764)
        {
            ScheduleTask(() =>
            {
                if (getOwner().IsAlreadyDead) return;
                // note: Java opened instance door 219 here for npc 217763; door control isn't exposed to
                // scripts yet.
                UseSkill(19967, level);
                ScheduleTask(() =>
                {
                    // note: Java deleted itself via AI2Actions.deleteOwner; no scripted despawn API
                    // exists yet.
                }, 3500);
            }, 3500);
        }
        else
        {
            UseSkill(19974, 46);
        }
    }

    // note: Java also overrode ask (CAN_RESIST_ABNORMAL = POSITIVE) — no C# equivalent hook exists.

    public override void OnDied()
    {
        if (getOwner().Template.NpcId == 217764)
        {
            // note: Java shouted 1401111 and 1401140 via NpcShoutsService here; NPC shouts aren't exposed
            // to scripts yet.
            var soul = GetNpc(217471);
            var sapping = GetNpc(217472);
            soul?.RemoveEffectBySkillId(19126);
            sapping?.RemoveEffectBySkillId(19126);
        }
        base.OnDied();
    }
}
