// NochsanaDoorAI2 — Java ai/instance/nochsanaTrainingCamp/NochsanaDoorAI2.java. Nochsana training
// camp door: never reacts to attacks, deletes itself immediately on death.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("nochsanadoor")]
public sealed class NochsanaDoorAI2 : GeneralNpcAI2
{
    public override void OnAttack(Creature creature)
    {
        // note: Java intentionally left this empty (no super call) — the door never reacts to being hit.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java immediately deleted itself (getController().onDelete()) on death; no delete-owner
        // action exists on NpcAi2 yet.
    }
}
