// ExplosionShadowsAI2 — Java ai/instance/aturamSkyFortress/ExplosionShadowsAI2.java. Trap prop:
// opens doors and casts a scripted skill sequence on aggro, then spawns a collector add on every
// affected player before deleting itself.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("explosion_shadows")]
public sealed class ExplosionShadowsAI2 : AggressiveNpcAI2
{
    public override void OnCreatureAggro(Creature creature)
    {
        base.OnCreatureAggro(creature);
        // note: Java cast skill 19428 on first aggro, opened instance doors 2/17, then after 3s cast
        // skill 19425, then after another 1.5s closed doors 17/2, visited every known player with the
        // 19502 abnormal effect to spawn a collector npc (799657) on them (removing the effect and
        // despawning the collector after 4s), and finally deleted itself via AI2Actions.deleteOwner.
        // Door control, SkillEngine casting, known-list visitation and effect queries aren't exposed to
        // scripts yet.
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        // note: Java only reset its "already triggered" flag here.
    }
}
