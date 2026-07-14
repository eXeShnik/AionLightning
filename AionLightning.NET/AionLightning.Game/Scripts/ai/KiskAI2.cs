// KiskAI2 — Java ai/KiskAI2.java. Player-placed bindstone (Kisk): broadcasts attack/death/removal
// notices to bound members and runs a bind-request confirmation flow.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("kisk")]
public sealed class KiskAI2 : NpcAi2
{
    public override void OnAttack(Creature attacker)
    {
        // note: Java broadcast STR_BINDSTONE_IS_ATTACKED to the Kisk's member list when at full HP;
        // Kisk/Kisk-member tracking and PacketSendUtility aren't ported to the script layer yet.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java broadcast an SM_EMOTION(DIE) + STR_BINDSTONE_IS_DESTROYED when the owner was already
        // dead; PacketSendUtility isn't wired at the script layer yet.
    }

    public override void OnDespawned()
    {
        // note: Java called KiskService.removeKisk and, unless already dead, broadcast
        // STR_BINDSTONE_IS_REMOVED; KiskService isn't ported yet.
    }

    public override void OnDialogStart(Player player)
    {
        // note: Java ran an AI2Request bind confirmation (SM_QUESTION_WINDOW) then KiskService.onBind
        // (also overrode pollInstance to refuse decay/respawn/reward — no C# equivalent poll exists).
        // Kisk/KiskService/AI2Request aren't ported to the script layer yet.
    }
}
