// VeraAI2 — Java ai/instance/ophidanBridge/VeraAI2.java. Vera: casts a self-buff skill on spawn.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("vera")]
public sealed class VeraAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java cast a self-buff skill (21438, "surkana buff") via SkillEngine on spawn; skill casting
        // isn't wired at the script layer yet.
    }
}
