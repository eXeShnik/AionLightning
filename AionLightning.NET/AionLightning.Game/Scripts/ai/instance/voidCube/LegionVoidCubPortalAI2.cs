// LegionVoidCubPortalAI2 — Java ai/instance/voidCube/LegionVoidCubPortalAI2.java. Void Cube legion
// portal: gates its use-dialog on the interacting player's legion owning the associated siege location.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("legion_voidcube_portal")]
public sealed class LegionVoidCubPortalAI2 : NpcAi2
{
    public override void OnThink()
    {
        // note: Java delegated to ThinkEventHandler.onThink — think loop owned by NpcAiService.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java delegated to DiedEventHandler.onDie.
    }

    public override void OnAttack(Creature creature)
    {
        // note: Java delegated to AttackEventHandler.onAttack (also overrode handleFinishAttack, dropped —
        // no C# equivalent hook exists).
    }

    public override void OnDialogStart(Player player)
    {
        // note: Java gated TalkEventHandler.onTalk/onFinishTalk on whether player.getLegion() owns siege
        // location 5011 (SiegeService.getSiegeLocation); legion/siege ownership isn't wired at the script
        // layer yet (siege system is largely unported — see migration_plan.md).
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
        // a no-op in NpcAi2).
    }

    public override void OnCreatureMoved(Creature creature)
    {
        // note: Java delegated to CreatureEventHandler.onCreatureMoved.
    }

    public override void OnDespawned()
    {
        // note: Java only called super.handleDespawned() here — no framework body to port. Also overrode
        // canHandleEvent (CREATURE_MOVED/CREATURE_NEEDS_SUPPORT gating via NPC_SHOUT_DATA/TRIBE_RELATIONS_DATA)
        // — no C# equivalent hook exists.
    }

    public override AttackIntention ChooseAttackIntention()
        // note: Java picked SWITCH_TARGET/SKILL_ATTACK/FINISH_ATTACK via AggroList.getMostHated() and
        // SkillAttackManager.chooseNextSkill(); aggro tracking and skill-attack selection are owned by
        // NpcAiService, not this script layer.
        => AttackIntention.SimpleAttack;
}
