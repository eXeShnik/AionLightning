namespace AionLightning.Game.Ai;

/// <summary>What an NPC should do about its current target next (Java <c>ai2.AttackIntention</c>).</summary>
public enum AttackIntention
{
    FinishAttack,
    SwitchTarget,
    SimpleAttack,
    SkillAttack,
    SkillBuff,
}
