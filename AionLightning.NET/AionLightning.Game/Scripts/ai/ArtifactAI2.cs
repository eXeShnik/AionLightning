// ArtifactAI2 — Java ai/ArtifactAI2.java. Siege artifact activation (item/legion-rights checks,
// SiegeService state machine, scheduled damage ticks to nearby enemies).
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("artifact")]
public sealed class ArtifactAI2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java ran a two-step AI2Request confirmation (SiegeService.getArtifact, item/legion-rights
        // checks) then, on activation, broadcast SM_ABYSS_ARTIFACT_INFO/SM_SYSTEM_MESSAGE state changes and
        // scheduled an ArtifactUseSkill tick loop that applied the artifact's skill to nearby enemies.
        // SiegeService/ArtifactLocation/AI2Request aren't ported to the script layer yet.
    }

    public override void OnDialogFinish(Player player)
    {
    }
}
