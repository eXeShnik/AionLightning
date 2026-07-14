// EnemyServantAI2 — Java ai/classNpc/EnemyServantAI2.java. Targets its creator's current target
// on spawn then loops an attack skill.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("enemyservant")]
public sealed class EnemyServantAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java targeted its creator's current target after 2s then looped skill 16907 (level 55)
        // every 6s via ThreadPoolManager (also overrode pollInstance to refuse decay/respawn/reward — no
        // C# equivalent poll exists). Creator-target resolution and repeating skill casts aren't wired at
        // the script layer yet (see NpcAi2.UseSkill).
    }
}
