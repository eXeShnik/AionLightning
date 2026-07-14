// TombGhostAI2 — Java ai/instance/shugoImperialTomb/TombGhostAI2.java. Shugo Imperial Tomb ghost:
// marks itself active on spawn and broadcasts a start emote; never thinks on its own.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("tombghost")]
// 219505, 219506, 219507
public sealed class TombGhostAI2 : AggressiveNpcAI2
{
    // note: Java also overrode canThink to always return false; no C# equivalent hook exists.

    public override void OnSpawned()
    {
        base.OnSpawned();
        Owner.State = CreatureState.Active;
        // note: Java also broadcast SM_EMOTION(START_EMOTE2); packet broadcasting isn't exposed to
        // scripts yet.
    }
}
