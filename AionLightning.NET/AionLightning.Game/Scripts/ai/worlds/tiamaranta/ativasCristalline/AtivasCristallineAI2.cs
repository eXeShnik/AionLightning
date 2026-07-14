// AtivasCristallineAI2 — Java ai/worlds/tiamaranta/ativasCristalline/AtivasCristallineAI2.java.
// HP-percentage-gated summons of Garnet/Topaz Komad adds.
using System;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("ativascristalline")]
public sealed class AtivasCristallineAI2 : AggressiveNpcAI2
{
    private bool _start90;
    private bool _start60;
    private bool _start30;
    private bool _start10;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckPercentage(Owner.HpPercentage);
    }

    public override void OnBackHome()
    {
        _start90 = _start60 = _start30 = _start10 = false;
        base.OnBackHome();
    }

    private void CheckPercentage(int hpPercentage)
    {
        if (hpPercentage <= 90)
        {
            if (!_start90) { _start90 = true; TopazKomad(); }
        }
        else if (hpPercentage <= 60)
        {
            if (!_start60) { _start60 = true; GarnetKomad(); }
        }
        else if (hpPercentage <= 30)
        {
            if (!_start30) { _start30 = true; TopazKomad(); }
        }
        else if (hpPercentage <= 10)
        {
            if (!_start10) { _start10 = true; GarnetKomad(); }
        }
    }

    private void GarnetKomad()
    {
        if (!Owner.IsAlreadyDead)
            RndSpawnInRange(282708, Random.Shared.Next(3, 6));
        // note: Java also checked getPosition().isSpawned() here; spawn-state isn't exposed on Position.
    }

    private void TopazKomad()
    {
        if (!Owner.IsAlreadyDead)
            RndSpawnInRange(282709, Random.Shared.Next(3, 6));
        // note: same isSpawned() gap as GarnetKomad.
    }

    private void RndSpawnInRange(int npcId, float distance)
    {
        float direction = Random.Shared.Next(0, 200) / 100f;
        float x1 = MathF.Cos(MathF.PI * direction) * distance;
        float y1 = MathF.Sin(MathF.PI * direction) * distance;
        Spawn(npcId, Owner.Position.X + x1, Owner.Position.Y + y1, Owner.Position.Z);
    }
}
