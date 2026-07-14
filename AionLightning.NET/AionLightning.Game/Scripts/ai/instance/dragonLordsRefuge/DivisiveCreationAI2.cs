// DivisiveCreationAI2 — Java ai/instance/dragonLordsRefuge/DivisiveCreationAI2.java. Tiamat add that
// picks a random nearby player and charges them shortly after spawning (and again after returning home).
using AionLightning.Game.Ai;

namespace Ai;

[AiName("divisivecreation")]
// 283139
public sealed class DivisiveCreationAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(AttackPlayer, 2000);
    }

    private void AttackPlayer()
    {
        // note: Java picked a random living known-player within 200m, set it as target, switched to
        // AiState.WALKING, issued a move-to-target order, and broadcast a START_EMOTE2 packet. Known-list
        // iteration, target assignment, and move-controller/emote-packet access aren't exposed to scripts
        // yet.
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        ScheduleTask(AttackPlayer, 2000);
    }
}
