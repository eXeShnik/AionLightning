// LegionMysticariumPortalAI2 — Java ai/instance/danuarMysticarium/LegionMysticariumPortalAI2.java.
// Legion siege portal: hand-rolled ai2-framework delegator (same shape as GeneralNpcAI2) gated by
// SiegeService legion ownership on dialog-start.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("legion_mysticarium_portal")]
public sealed class LegionMysticariumPortalAI2 : NpcAi2
{
    public override void OnThink()
    {
        // note: Java delegated to ThinkEventHandler.onThink — the NPC think loop is owned by
        // NpcAiService.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java delegated to DiedEventHandler.onDie — death handling is owned by NpcAiService.
    }

    public override void OnAttack(Creature creature)
    {
        // note: Java delegated to AttackEventHandler.onAttack (also overrode
        // handleCreatureNeedsSupport/handleFinishAttack/handleTargetChanged/handleMoveValidate/
        // canHandleEvent, dropped — no C# equivalent hooks exist).
    }

    public override void OnDialogStart(Player player)
    {
        // note: Java only opened the portal dialog (TalkEventHandler.onTalk) for a player whose
        // legion owned siege location 5011 (SiegeService.getSiegeLocation), otherwise ran the
        // finish-talk path; SiegeService/SiegeLocation aren't exposed to the script layer yet.
    }

    public override void OnDialogFinish(Player player)
    {
        // note: Java delegated to TalkEventHandler.onFinishTalk.
    }

    public override void OnAttackComplete()
    {
        // note: Java delegated to AttackEventHandler.onAttackComplete.
    }

    public override void OnTargetReached()
    {
        // note: Java delegated to TargetEventHandler.onTargetReached.
    }

    public override void OnNotAtHome()
    {
        // note: Java delegated to ReturningEventHandler.onNotAtHome.
    }

    public override void OnBackHome()
    {
        // note: Java delegated to ReturningEventHandler.onBackHome.
    }

    public override void OnTargetTooFar()
    {
        // note: Java delegated to TargetEventHandler.onTargetTooFar.
    }

    public override void OnTargetGiveup()
    {
        // note: Java delegated to TargetEventHandler.onTargetGiveup.
    }

    public override void OnMoveArrived()
    {
        // note: Java delegated to MoveEventHandler.onMoveArrived (also called
        // super.handleMoveArrived(), which is a no-op in NpcAi2).
    }

    public override void OnCreatureMoved(Creature creature)
    {
        // note: Java delegated to CreatureEventHandler.onCreatureMoved.
    }

    public override void OnDespawned()
    {
        // note: Java only called super.handleDespawned() here — no framework body to port.
    }

    public override AttackIntention ChooseAttackIntention()
        // note: Java picked SWITCH_TARGET/SKILL_ATTACK/FINISH_ATTACK via AggroList.getMostHated() and
        // SkillAttackManager.chooseNextSkill(); aggro tracking and skill-attack selection are owned by
        // NpcAiService, not this script layer.
        => AttackIntention.SimpleAttack;
}
