// PazuzuWormAI2 — Java ai/instance/abyssal_splinter/PazuzuWormAI2.java. Pazuzu's worm add: targets
// Pazuzu and casts a skill on it 3s after spawn.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("pazuzuworm")]
public sealed class PazuzuWormAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java scheduled a 3s-delayed action that targeted Pazuzu (219942) via
        // AI2Actions.targetCreature and cast skill 19291 via AI2Actions.useSkill. AI2Actions and
        // instance npc lookup aren't exposed to scripts yet.
    }
}
