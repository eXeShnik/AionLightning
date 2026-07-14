// ActionItemNpcAI2 — Java ai/ActionItemNpcAI2.java. Root base for "use item on NPC" flows
// (chest/shifter): shows a progress bar via SM_USE_OBJECT/SM_EMOTION, then invokes a finish hook.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("useitem")]
public class ActionItemNpcAI2 : NpcAi2
{
    protected int startBarAnimation = 1;
    protected int cancelBarAnimation = 2;

    public override void OnDialogStart(Player player) => HandleUseItemStart(player);

    /// <summary>Java <c>handleUseItemStart</c>: staged an SM_USE_OBJECT progress bar (with an
    /// ItemUseObserver abort path) for <see cref="GetTalkDelay"/> ms before calling
    /// <see cref="HandleUseItemFinish"/>.</summary>
    protected virtual void HandleUseItemStart(Player player)
    {
        // note: Java's progress-bar packets (SM_USE_OBJECT/SM_EMOTION) and its cancel-on-move
        // ItemUseObserver aren't wired at the script layer yet — PacketSendUtility doesn't exist here.
        HandleUseItemFinish(player);
    }

    /// <summary>Java <c>handleUseItemFinish</c>: delegated to <c>AI2Actions.handleUseItemFinish</c> when
    /// the owning NPC is inside an instance.</summary>
    protected virtual void HandleUseItemFinish(Player player)
    {
        // note: Java delegated to AI2Actions.handleUseItemFinish(this, player) when getOwner().isInInstance();
        // AI2Actions has no ported equivalent.
    }

    /// <summary>Java <c>getTalkDelay</c>: <c>NpcObjectTemplate.getTalkDelay() * 1000</c>.</summary>
    protected virtual int GetTalkDelay() => 0; // note: talk-delay isn't exposed on NpcTemplate yet.
}
