// LordLanmarkAI2 — Java ai/instance/admaStronghold/LordLanmarkAI2.java. Lord Lanmark: starts walking
// its "lordlanmark" route ~1s after spawning.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("lordlanmark")]
public sealed class LordLanmarkAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java scheduled (1s delay) setting the spawn's walker id to "lordlanmark" and starting
        // WalkManager movement with a start-emote broadcast; npc-walker routes aren't wired at the script
        // layer yet.
    }
}
