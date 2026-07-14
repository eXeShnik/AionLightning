// FirstTiamatAI2 — Java ai/instance/dragonLordsRefuge/FirstTiamatAI2.java. Tiamat's opening-cutscene
// stand-in: casts an opening skill on spawn.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("firsttiamat")]
// 219360
public sealed class FirstTiamatAI2 : AggressiveNpcAI2
{
    // note: Java also overrode handleDeactivate as a no-op — activate/deactivate switching was pure
    // ai2-framework plumbing with no C# equivalent.

    public override void OnSpawned()
    {
        base.OnSpawned();
        if (getOwner().Template.NpcId == 219360)
        {
            UseSkill(20917);
        }
    }
}
