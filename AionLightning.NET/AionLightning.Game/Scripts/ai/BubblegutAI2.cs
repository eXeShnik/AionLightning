// BubblegutAI2 — Java ai/BubblegutAI2.java. Casts a fixed skill on spawn.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("bubblegut")]
public sealed class BubblegutAI2 : GeneralNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        UseSkill(16447);
    }
}
