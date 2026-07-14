// BeritraIronFenceAI2 — Java ai/instance/kamarBattlefield/BeritraIronFenceAI2.java. Inert fence
// prop that deletes itself on death.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("beritraironfence")]
public sealed class BeritraIronFenceAI2 : NoActionAI2
{
    public override void OnDied()
    {
        base.OnDied();
        // note: Java deleted itself via AI2Actions.deleteOwner; AI2Actions isn't exposed to scripts yet
        // (also overrode canThink() to return false — no C# equivalent hook exists).
    }
}
