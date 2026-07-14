// LediarAssistantAI2 — Java ai/instance/shugoImperialTomb/LediarAssistantAI2.java. Shugo Imperial
// Tomb helper: adds hate toward the tomb's defense towers on spawn.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("lediar_assistant")]
// 219461
public sealed class LediarAssistantAI2 : AggressiveNpcAI2
{
    private static readonly int[] NpcIds = { 831251, 831250, 831305 };

    public override void OnSpawned()
    {
        AddHate();
        base.OnSpawned();
    }

    private void AddHate()
    {
        // note: Java also called EmoteManager.emoteStopAttacking(getOwner()) here; not exposed to
        // scripts yet.
        foreach (int npcId in NpcIds)
        {
            var tower = GetNpc(npcId);
            if (tower is not null && !tower.IsAlreadyDead)
                Owner.AddHate(tower.ObjectId, 10000);
        }
    }

    // note: Java also overrode modifyOwnerDamage to clamp all damage to 1; no damage-modification hook
    // exists on NpcAi2 yet.
}
