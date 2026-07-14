// ThePlaguebearerAI2 — Java ai/instance/beshmundirTemple/ThePlaguebearerAI2.java. Spawns a single
// "Plaguebearer Fragment" add near itself at each of four HP breakpoints.
using System;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("theplaguebearer")]
public sealed class ThePlaguebearerAI2 : AggressiveNpcAI2
{
    private static readonly int[] FragmentNpcIds = { 281808, 281809 };

    private bool _isStart;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckPercentage(Owner.HpPercentage);
    }

    public override void OnBackHome()
    {
        _isStart = false;
        base.OnBackHome();
    }

    private void CheckPercentage(int hpPercentage)
    {
        if (hpPercentage is 90 or 70 or 50 or 30)
        {
            _isStart = true;
            Summon();
        }
    }

    private void Summon()
    {
        // note: Java also required getPosition().isSpawned() (owner still in world); no C# equivalent exists.
        if (Owner.IsAlreadyDead || !_isStart) return;
        var distance = Random.Shared.Next(4, 11);
        var npcId = FragmentNpcIds[Random.Shared.Next(2)];
        RandomSpawnInRange(npcId, distance);
    }

    private void RandomSpawnInRange(int npcId, float distance)
    {
        var direction = Random.Shared.Next(0, 200) / 100f;
        var x = MathF.Cos(MathF.PI * direction) * distance;
        var y = MathF.Sin(MathF.PI * direction) * distance;
        Spawn(npcId, Owner.Position.X + x, Owner.Position.Y + y, Owner.Position.Z);
    }
}
