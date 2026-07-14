// SiegeWeaponAI2 — Java ai/siege/SiegeWeaponAI2.java. Player-summoned siege weapon: guards until
// commanded to attack, then casts its single npc-skill template against castle-door races matching
// its master's faction.
// note: Java extended AISummon (a framework class, not a script) — this port extends NpcAi2 instead.
// Java also overrode handleFollowMe/handleStopFollowMe/handleMoveValidate (no NpcAi2 equivalents) and
// getController/getNpcSkillTemplates (framework-only helpers with no C# counterpart) — all dropped.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("siege_weapon")]
public sealed class SiegeWeaponAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        // note: Java set state to IDLE, switched into SummonsService.GUARD mode, and read its
        // NpcSkillTemplate (skill/level/duration) from SiegeWeaponController. SiegeWeaponController and
        // NpcSkillTemplates don't apply to a plain Npc owner (the Player-owned Summon/SummonsService model
        // is a different object type) — not exposed here.
    }

    public override void OnCreatureMoved(Creature creature)
    {
        // note: Java's handleCreatureMoved override was already empty.
    }

    public override void OnTargetTooFar()
    {
        // note: Java resumed moving toward its destination (MoveController.moveToDestination); movement
        // control isn't exposed to the script layer yet.
    }

    public override void OnMoveArrived()
    {
        // note: Java notified its controller (onMove) and aborted the move controller; not exposed here.
    }

    public override void OnAttack(Creature creature)
    {
        // note: Java only attacked castle-door races matching its Player master's race (via
        // SiegeWeaponController's owning master), gated by SummonMode.ATTACK and a per-skill-duration + 2s
        // cooldown. Master/race/mode aren't exposed on a plain Npc owner.
    }
}
