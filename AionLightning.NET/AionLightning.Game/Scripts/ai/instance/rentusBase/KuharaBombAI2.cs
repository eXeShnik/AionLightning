// KuharaBombAI2 — Java ai/instance/rentusBase/KuharaBombAI2.java. Suicide-bomb add: follows its route,
// then detonates a skill on the boss once it arrives.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("kuhara_bomb")]
public sealed class KuharaBombAI2 : GeneralNpcAI2
{
    private bool _isDestroyed;
    private Npc? _boss;

    public override void OnSpawned()
    {
        base.OnSpawned();
        State = AiState.Following;
        _boss = GetNpc(217311);
    }

    public override void OnMoveArrived()
    {
        base.OnMoveArrived();
        if (_isDestroyed) return;
        _isDestroyed = true;
        if (_boss is { IsAlreadyDead: false }) UseSkill(19659);
    }
}
