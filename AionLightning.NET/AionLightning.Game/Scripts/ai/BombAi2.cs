// BombAi2 — Java ai/BombAi2.java. Self-destructing bomb NPC: casts a skill after a delay, then
// deletes itself.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("bomb")]
public sealed class BombAi2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java read a BombTemplate (AI_DATA.getAiTemplate().getBombs()) and, after a 2s + template-cd
        // delay, self-targeted and cast the bomb skill (AI2Actions.targetSelf/useSkill) then deleted the
        // owner (AI2Actions.deleteOwner) once the skill's duration elapsed. Bomb templates, self-targeting,
        // and delete-on-timer aren't exposed to scripts yet (also overrode pollInstance to refuse decay/
        // respawn/reward — no C# equivalent poll exists).
    }
}
