// LimbRenderAI2 — Java ai/worlds/tiamarantasEye/LimbRenderAI2.java. Shouts on every attack and casts a
// special skill every 100th hit.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("limbrender")]
public sealed class LimbRenderAI2 : GeneralNpcAI2
{
    private int _attackCount;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        _attackCount++;
        SendMsg(1401462);
        if (_attackCount == 100)
        {
            _attackCount = 0;
            UseSkill(20655);
            SendMsg(1401463);
        }
        // note: Java also overrode modifyDamage to clamp incoming damage to 1; no damage-modification
        // hook exists on NpcAi2 yet.
    }

    public override void OnSpawned()
    {
        base.OnSpawned();
        SendMsg(1401461);
    }
}
