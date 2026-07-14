// AlarmAI2 — Java ai/instance/aturamSkyFortress/AlarmAI2.java. Trap prop: triggers a scripted
// walk-and-alert sequence when a player gets close, then despawns itself.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("alarm")]
public sealed class AlarmAI2 : AggressiveNpcAI2
{
    public override void OnCreatureMoved(Creature creature)
    {
        base.OnCreatureMoved(creature);
        // note: Java checked if a moving Player came within 23m and, on first trigger, disabled its own
        // think loop (canThink, no C# equivalent), shouted twice, switched to a scripted walk route
        // (walker id 3002400002), broadcast an emote, opened instance doors 128/138, and deleted itself
        // via AI2Actions.deleteOwner after 3s. MathUtil distance, NpcShoutsService, WalkManager and door
        // control aren't exposed to scripts yet.
    }
}
