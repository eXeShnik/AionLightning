// DeliveryManAI2 — Java ai/DeliveryManAI2.java. Temporary courier NPC: follows the player who
// summoned it, opens their mailbox on talk, and self-deletes after a service window.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("deliveryman")]
public sealed class DeliveryManAI2 : FollowingNpcAI2
{
    // note: Java's handleCustomEvent(EVENT_SET_CREATOR) had no ported hook, so _owner has no wiring path
    // yet — kept so the follow/dialog logic below stays structurally faithful once that lands.
    public const int EventSetCreator = 1;
    private const int ServiceTimeMs = 5 * 60 * 1000;
    private const int SpawnActionDelayMs = 1500;

    private Player? _owner;

    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(() =>
        {
            // note: Java deleted the owner via AI2Actions.deleteOwner; no scripted despawn API exists yet.
        }, ServiceTimeMs);
        ScheduleTask(() =>
        {
            // note: Java broadcast an SM_SYSTEM_MESSAGE shout (390266) here; PacketSendUtility/NpcShout
            // aren't wired at the script layer yet.
            if (_owner is not null)
            {
                HandleFollowMe(_owner);
                OnCreatureMoved(_owner);
            }
        }, SpawnActionDelayMs);
    }

    public override void OnDespawned()
    {
        // note: Java broadcast an SM_SYSTEM_MESSAGE shout (390267) on despawn; PacketSendUtility/NpcShout
        // aren't wired at the script layer yet.
    }

    public override void OnDialogStart(Player player)
    {
        // note: Java opened the mailbox dialog (SM_DIALOG_WINDOW + Mailbox.sendMailList) only for _owner;
        // Player.Mailbox and SM_DIALOG_WINDOW aren't ported to the script layer yet.
    }
}
