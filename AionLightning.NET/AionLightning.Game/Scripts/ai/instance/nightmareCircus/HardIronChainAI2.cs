// HardIronChainAI2 — Java ai/instance/nightmareCircus/HardIronChainAI2.java. Self-deletes on death.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("hard_iron_chain")]
public sealed class HardIronChainAI2 : NoActionAI2
{
    public override void OnDied()
    {
        base.OnDied();
        // note: Java self-deleted via AI2Actions.deleteOwner; self-delete isn't wired at the script
        // layer yet.
    }
}
