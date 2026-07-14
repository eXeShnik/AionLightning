// ChuraTwinbladeAI2 — Java ai/instance/udasTempleLower/ChuraTwinbladeAI2.java. Chura Twinblade boss:
// shouts and briefly disengages into a scripted skill-use/re-target sequence at 50% HP.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("churatwinblade")]
public sealed class ChuraTwinbladeAI2 : AggressiveNpcAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: Java shouted message 1500028 and called callForHelp(60) on the first attack after
        // spawn/back-home, then at 50% HP disengaged into a scripted sequence (AI2Actions.useSkill(18624),
        // a 2.5s re-target step and a 5s room-wide retarget step driven by AggroList/getMoveController/
        // getGameStats via ThreadPoolManager, also gated by a canThink()-guarded thread with no C#
        // equivalent); aggro-list access and the walking/skill-use choreography aren't wired at the script
        // layer yet.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java shouted message 1500030 via NpcShoutsService on death.
    }
}
