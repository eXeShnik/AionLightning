// SacrificialSoulAI2 — Java ai/instance/beshmundirTemple/SacrificialSoulAI2.java. Add that follows
// toward the boss and self-detonates on it once it arrives.
using System.Linq;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("templeSoul")]
public sealed class SacrificialSoulAI2 : GeneralNpcAI2
{
    private const int BossNpcId = 216263;
    private const int SpawnEffectSkillId = 18901;
    private const int ExplosionMarkSkillId = 18959;
    private const int SelfDetonateSkillId = 18960;

    private Npc? _boss;

    public override void OnSpawned()
    {
        base.OnSpawned();
        UseSkill(SpawnEffectSkillId);
        State = AiState.Following;
        _boss = GetNpc(BossNpcId);
        // note: Java targeted the boss and moved toward it via AI2Actions.targetCreature +
        // getMoveController().moveToTargetObject(); movement/targeting control isn't exposed to scripts —
        // NpcAiService owns movement.
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (creature.GetActiveEffects().Any(e => e.SkillId == ExplosionMarkSkillId))
        {
            // note: Java aborted its move then deleted itself via AI2Actions.deleteOwner; no scripted
            // move-abort/despawn API exists yet.
        }
    }

    public override void OnMoveArrived()
    {
        base.OnMoveArrived();
        if (_boss is not null && !_boss.IsAlreadyDead)
        {
            // note: Java cast this on the boss explicitly (SkillEngine.useNoAnimationSkill with a target)
            // then deleted itself via AI2Actions.deleteOwner; explicit-target skill casting and despawn
            // aren't wired at the script layer yet.
            UseSkill(SelfDetonateSkillId);
        }
    }
}
