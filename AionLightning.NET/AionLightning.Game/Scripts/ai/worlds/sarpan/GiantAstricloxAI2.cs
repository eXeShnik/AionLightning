// GiantAstricloxAI2 — Java ai/worlds/sarpan/GiantAstricloxAI2.java. Spawns 6 fixed-position adds on
// death.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("giantastriclox")]
public sealed class GiantAstricloxAI2 : NpcAi2
{
    public override void OnDied()
    {
        base.OnDied();
        Spawn(730495, 796.7448f, 867.9318f, 675.22473f, 36);
        Spawn(730495, 794.9168f, 869.0062f, 675.06616f, 34);
        Spawn(730495, 796.2312f, 871.0012f, 674.43726f, 25);
        Spawn(730495, 799.8763f, 869.46265f, 674.75934f, 44);
        Spawn(730495, 802.3064f, 867.8118f, 675.19116f, 46);
        Spawn(730495, 798.8771f, 870.45953f, 674.51013f, 39);
        // note: Java also overrode modifyDamage to clamp incoming damage to 1; no damage-modification
        // hook exists on NpcAi2 yet.
    }
}
