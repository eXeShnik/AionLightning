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
        // note: Java broadcast STR_BINDSTONE_IS_ATTACKED to the Kisk's member list when at full HP.
        // Services.KiskService is reachable now via GetKisk(), but there's no script-layer helper to
        // send an arbitrary SM_SYSTEM_MESSAGE to a set of players (only SendMsg's NPC-shout format) —
        // this hook is also not yet invoked by NpcAiService's combat loop (see NpcAi2's OnAttack doc).
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java broadcast an SM_EMOTION(DIE) + STR_BINDSTONE_IS_DESTROYED when the owner was
        // already dead. Same script-layer message-send gap as OnAttack above; also not yet invoked by
        // the engine (see OnDespawned below, which — unlike this hook — has a real caller: KiskService's
        // own despawn timer / ConsumeResurrectionAsync's last-resurrect path already call
        // KiskService.RemoveKiskAsync directly, so the cleanup itself doesn't depend on this hook firing).
    }

    public override void OnDespawned()
    {
        // Java KiskService.getInstance().removeKisk(getOwner()). Not currently invoked by the engine
        // (NpcAiService has no generic "NPC despawned" dispatch to ScriptedAi yet — see NpcAi2 class
        // doc), but wired here so it fires correctly once that dispatch lands. The kisk's actual
        // despawn/cleanup already happens unconditionally via KiskService's own timer, independent of
        // this hook.
        RemoveKisk();
    }

    public override void OnDialogStart(Player player)
    {
        // note: Java ran an AI2Request bind confirmation (SM_QUESTION_WINDOW) then KiskService.onBind
        // when getOwner().getMaxMembers() > 1 (also overrode pollInstance to refuse decay/respawn/
        // reward — no C# equivalent poll exists, and this dialog hook isn't invoked by any packet
        // handler yet). KiskService.SpawnKiskAsync currently auto-binds the placer immediately instead
        // of waiting for this confirmation dialog — see its class doc note.
    }
}
