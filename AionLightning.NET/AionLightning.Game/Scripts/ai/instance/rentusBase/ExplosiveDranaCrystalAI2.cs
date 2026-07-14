// ExplosiveDranaCrystalAI2 — Java ai/instance/rentusBase/ExplosiveDranaCrystalAI2.java. Use-item prop:
// strips one of the boss's shield stages and spawns follow-up npcs, or self-destructs after a minute if
// never used.
using System.Linq;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("explosive_drana_crystal")]
public sealed class ExplosiveDranaCrystalAI2 : ActionItemNpcAI2
{
    private bool _isUsed;

    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(Despawn, 60000);
    }

    protected override void HandleUseItemFinish(Player player)
    {
        if (_isUsed) return;
        _isUsed = true;
        var boss = GetNpc(217308);
        if (boss is { IsAlreadyDead: false })
        {
            foreach (var skillId in new[] { 19370, 19371, 19372 })
            {
                if (boss.GetActiveEffects().Any(e => e.SkillId == skillId))
                {
                    boss.RemoveEffectBySkillId(skillId);
                    break;
                }
            }
        }
        var p = Owner.Position;
        var npc = Spawn(282530, p.X, p.Y, p.Z, (byte)p.Heading);
        Spawn(282529, p.X, p.Y, p.Z, (byte)p.Heading);
        if (npc is not null) UseSkill(19373);
        // note: Java also cast skill 19654 on the invisible marker npc, deleted it (NpcActions.delete),
        // then called AI2Actions.deleteOwner(this) to remove this crystal — scripted NPC delete isn't
        // exposed to scripts yet.
    }

    private void Despawn()
    {
        if (Owner.IsAlreadyDead) return;
        // note: Java called AI2Actions.deleteOwner(null) here — scripted NPC delete isn't exposed yet.
    }

    public override void OnDied()
    {
        base.OnDied();
    }

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }
}
