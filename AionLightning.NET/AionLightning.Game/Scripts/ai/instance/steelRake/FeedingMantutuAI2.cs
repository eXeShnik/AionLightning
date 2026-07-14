// FeedingMantutuAI2 — Java ai/instance/steelRake/FeedingMantutuAI2.java. Use-item supply device
// that blocks its dialog while a feed/water device is already out, and signals the mantutu boss
// after spawning the matching supply on use.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("feeding_mantutu")]
public sealed class FeedingMantutuAI2 : ShifterAI2
{
    public override void OnDialogStart(Player player)
    {
        if (GetNpc(281128) is null && GetNpc(281129) is null)
            base.OnDialogStart(player);
    }

    protected override void HandleUseItemFinish(Player player)
    {
        base.HandleUseItemFinish(player);
        var boss = GetNpc(219033);
        if (boss is not null && !boss.IsAlreadyDead)
        {
            _ = Owner.Template.NpcId switch
            {
                701387 => Spawn(281129, 712.042f, 490.5559f, 939.7027f, 0), // water supply
                701386 => Spawn(281128, 714.62634f, 504.4552f, 939.60675f, 0), // feed supply
                _ => null
            };
            // note: Java forwarded the spawned supply npc to the boss via boss.getAi2().onCustomEvent(1, npc)
            // then self-deleted via AI2Actions.deleteOwner(this); custom AI events and self-delete aren't
            // wired at the script layer yet.
        }
    }
}
