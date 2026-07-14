// cubeMineAI2 — Java ai/instance/voidCube/cubeMineAI2.java. Void Cube trap mine ("rootmine"): checks
// line-of-sight/range to nearby players on sight/move and detonates a mine skill (stasis/destructive).
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("rootmine")]
public sealed class cubeMineAI2 : AggressiveNpcAI2
{
    public override void OnCreatureSee(Creature creature)
    {
        // note: Java delegated to checkDistance (MathUtil.isIn3dRange + GeoService.canSee + AI2Actions
        // targetCreature/useSkill + SkillEngine.applyEffectDirectly for the stasis/destructive mine skill,
        // then getOwner().getController().die()); geodata line-of-sight and skill-cast wiring aren't
        // ported at the script layer yet. Also overrode pollInstance (SHOULD_DECAY/RESPAWN/REWARD all
        // NEGATIVE) — no C# equivalent poll exists.
    }

    public override void OnCreatureMoved(Creature creature)
    {
        // note: same checkDistance path as OnCreatureSee.
    }
}
