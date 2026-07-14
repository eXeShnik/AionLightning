// GeneralNpcAI2 — Java ai/GeneralNpcAI2.java. Root ai2 delegator: every hook forwarded to a
// framework *EventHandler owned by the (dropped) ai2 engine. NPC think/attack/dialog/movement
// behaviour lives in NpcAiService today; this class exists so the inheritance chain (Aggressive/
// Following/etc.) and the "general" ai-name compile and register.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("general")]
public class GeneralNpcAI2 : NpcAi2
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
        // note: Java delegated to TalkEventHandler.onTalk.
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
        // SkillAttackManager.chooseNextSkill(); aggro tracking and skill-attack selection are owned by
        // NpcAiService, not this script layer.
        => AttackIntention.SimpleAttack;
}
