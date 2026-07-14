// KaluvaSpawnAI2 — Java ai/instance/abyssal_splinter/KaluvaSpawnAI2.java. Kaluva's egg add: hatches
// into one of 4 add formations 22s after spawn (when Kaluva's debuff ends), then deletes itself.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("kaluvaspawn")]
public sealed class KaluvaSpawnAI2 : NpcAi2
{
    public override void OnDied()
    {
        base.OnDied();
        // note: Java cancelled its hatch task, removed Kaluva's debuff effect (19152) if Kaluva was
        // still alive, and deleted itself via AI2Actions.deleteOwner. Instance npc lookup, effect removal
        // and AI2Actions aren't exposed to scripts yet.
    }

    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java scheduled a 22s hatch task spawning one of 4 add formations (281911/281912/282057
        // in various counts) at its own position, then repeated the Kaluva-debuff cleanup + self-delete
        // from OnDied.
    }
}
