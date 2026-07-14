// LegionIdgelPortalAI2 — Java ai/instance/idgelResearchCenter/LegionIdgelPortalAI2.java. Idgel
// Research Center legion-siege portal: every combat/dialog/movement hook forwarded to a framework
// EventHandler, with a legion-siege ownership gate on dialog start.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("legion_idgel_portal")]
public sealed class LegionIdgelPortalAI2 : NpcAi2
{
    public override void OnThink()
    {
        // note: Java delegated to ThinkEventHandler.onThink — the NPC think loop is owned by NpcAiService.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java delegated to DiedEventHandler.onDie — death handling is owned by NpcAiService.
    }

    public override void OnAttack(Creature creature)
    {
        // note: Java delegated to AttackEventHandler.onAttack (also overrode handleFinishAttack, dropped —
        // no C# equivalent hook exists).
    }

    public override void OnDialogStart(Player player)
    {
        // note: Java checked whether the player's legion owned siege location 5011 (SiegeService) before
        // delegating to TalkEventHandler.onTalk/onFinishTalk. SiegeLocation/legion-ownership checks and
        // TalkEventHandler aren't exposed to scripts yet.
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
        // note: Java delegated to MoveEventHandler.onMoveArrived (also called super.handleMoveArrived(),
        // which is a no-op in NpcAi2).
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
        // NpcSkillEntry/SkillAttackManager.chooseNextSkill(); aggro tracking and skill-attack selection are
        // owned by NpcAiService, not this script layer (also overrode canHandleEvent, handleTargetChanged
        // and handleMoveValidate — none have a C# equivalent hook).
        => AttackIntention.SimpleAttack;
}
