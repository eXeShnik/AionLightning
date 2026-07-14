// DredgionCommanderAI2 — Java ai/siege/DredgionCommanderAI2.java. Dredgion instance commander:
// every 45s casts an instance-specific skill against its current target when that target is a
// dragon general (GCHIEF_DARK/GCHIEF_LIGHT race) and still alive.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("dredgionCommander")]
public sealed class DredgionCommanderAI2 : SiegeNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(CastAgainstDragonGeneral, 45000, 45000);
    }

    private void CastAgainstDragonGeneral()
    {
        // note: Java looked up a per-npcId skill (276649->17572, 276871/276872->18411, 258236->18428,
        // 272294->21312) and cast it against the owner's current target (if it was an Npc of race
        // GCHIEF_DARK/GCHIEF_LIGHT and not already dead) via AI2Actions.useSkill + AggroList.addHate(10000).
        // Race.GCHIEF_DARK/GCHIEF_LIGHT have no C# equivalent (Race only models ASMODIANS/ELYOS/PC_ALL), so
        // the dragon-general check can't be ported.
    }
}
