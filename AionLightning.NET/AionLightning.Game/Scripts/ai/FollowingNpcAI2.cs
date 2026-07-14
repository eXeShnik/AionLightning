// FollowingNpcAI2 — Java ai/FollowingNpcAI2.java. Root base for NPCs that follow a creature
// (e.g. DeliveryManAI2 following its owning player).
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("following")]
public class FollowingNpcAI2 : GeneralNpcAI2
{
    /// <summary>Java <c>handleFollowMe</c>: starts following <paramref name="creature"/>.</summary>
    protected virtual void HandleFollowMe(Creature creature)
    {
        // note: Java delegated to FollowEventHandler.follow — target-follow movement isn't scripted yet,
        // it stays owned by NpcAiService.
    }

    public override void OnCreatureMoved(Creature creature)
    {
        // note: Java re-issued FollowEventHandler.creatureMoved/stopFollow depending on whether
        // getOwner().getTarget() still matched creature; that follow-state isn't tracked at the script
        // layer yet.
    }

    /// <summary>Java <c>handleStopFollowMe</c>: stops following <paramref name="creature"/>.</summary>
    protected virtual void HandleStopFollowMe(Creature creature)
    {
        // note: Java delegated to FollowEventHandler.stopFollow.
    }
}
