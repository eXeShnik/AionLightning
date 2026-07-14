// ShirikRegulatorAI2 — Java ai/instance/muadasTrencher/ShirikRegulatorAI2.java. On death, spawns a
// follow-up NPC at its own position.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("shirik_regulator")]
public sealed class ShirikRegulatorAI2 : AggressiveNpcAI2
{
    public override void OnDied()
    {
        Spawn(282539, Owner.Position.X, Owner.Position.Y, Owner.Position.Z);
        base.OnDied();
        // note: Java called AI2Actions.deleteOwner(this) here too; also overrode pollInstance() to
        // refuse decay/respawn/reward — the AIQuestion poll framework has no C# equivalent.
    }
}
