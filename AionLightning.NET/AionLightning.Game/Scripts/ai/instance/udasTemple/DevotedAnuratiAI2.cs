// DevotedAnuratiAI2 — Java ai/instance/udasTemple/DevotedAnuratiAI2.java. Devoted Anurati boss:
// HP-threshold shout + scripted skill-use/helper-spawn sequence, tracked helper despawn on reset/death.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("devotedanurati")]
public sealed class DevotedAnuratiAI2 : AggressiveNpcAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: Java shouted message 1500031 on the first attack after spawn/back-home, then at 80/65/45/
        // 35/25/15/5% HP shouted 1500032 and ran a scripted disengage (AI2Actions.useSkill(18745), a 2s
        // walking step and a 6s re-target step driven by AggroList/getMoveController/getGameStats) plus a
        // tracked-helper spawn (SpawnEngine + moveToForward); none of that choreography is wired at the
        // script layer yet.
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        // note: Java despawned tracked helper spawns here.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java despawned tracked helper spawns and shouted message 1500033 on death.
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
        // note: Java despawned tracked helper spawns here too.
    }
}
