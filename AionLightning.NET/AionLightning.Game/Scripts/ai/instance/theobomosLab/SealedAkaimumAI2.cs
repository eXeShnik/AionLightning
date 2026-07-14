// SealedAkaimumAI2 — Java ai/instance/theobomosLab/SealedAkaimumAI2.java. Sealed Akaimum: starts
// walking its "sealedakaimum" route ~1s after spawning.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("sealedakaimum")]
public sealed class SealedAkaimumAI2 : GeneralNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java scheduled (1s delay) setting the spawn's walker id to "sealedakaimum" and starting
        // WalkManager movement with a start-emote broadcast; npc-walker routes aren't wired at the script
        // layer yet.
    }
}
