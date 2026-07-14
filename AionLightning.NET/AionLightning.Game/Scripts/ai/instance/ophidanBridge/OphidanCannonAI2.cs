// OphidanCannonAI2 — Java ai/instance/ophidanBridge/OphidanCannonAI2.java. Ophidan Bridge cannon:
// one-shot use casts a race-specific morph skill, then schedules its own respawn/delete.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("ophidan_bridge_cannon")]
public sealed class OphidanCannonAI2 : ActionItemNpcAI2
{
    private bool _used;

    protected override void HandleUseItemFinish(Player player)
    {
        if (_used) return;
        _used = true;
        // note: Java cast a race-specific morph skill (21434 elyos / 21435 asmodian) via SkillEngine, then
        // scheduled a respawn and deleted itself (AI2Actions). Also overrode pollInstance (SHOULD_REWARD
        // NEGATIVE) — no C# equivalent poll exists. Skill-casting and scheduled respawn/delete-owner
        // actions aren't wired at the script layer yet.
    }
}
