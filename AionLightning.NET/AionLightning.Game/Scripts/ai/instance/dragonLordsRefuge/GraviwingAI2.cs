// GraviwingAI2 — Java ai/instance/dragonLordsRefuge/GraviwingAI2.java. Dragon Lords' Refuge add:
// casts an enrage skill once either empyrean god has died.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("graviwing")]
// 219366
public sealed class GraviwingAI2 : AggressiveNpcAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        IsDeadGod();
    }

    private bool IsDeadGod()
    {
        var marcutan = GetNpc(219491);
        var kaisinel = GetNpc(219488);
        if (IsDead(marcutan) || IsDead(kaisinel))
        {
            UseSkill(20983);
            return true;
        }
        return false;
    }

    private static bool IsDead(Npc? npc) => npc is not null && npc.IsAlreadyDead;
}
