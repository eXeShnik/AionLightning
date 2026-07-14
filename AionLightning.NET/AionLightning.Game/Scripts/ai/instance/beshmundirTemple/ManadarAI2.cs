// ManadarAI2 — Java ai/instance/beshmundirTemple/ManadarAI2.java. Below 90% HP, repeatedly spawns
// adds in a ring around itself every 6 seconds.
using System;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("manadar")]
public sealed class ManadarAI2 : AggressiveNpcAI2
{
    private static readonly int[] SpawnNpcIds = { 281545, 281756 };
    private const int CheckIntervalMs = 6000;

    private bool _isStart;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (!_isStart && Owner.HpPercentage <= 90)
        {
            _isStart = true;
            Check();
        }
    }

    public override void OnBackHome()
    {
        _isStart = false;
        base.OnBackHome();
    }

    private void Check()
    {
        // note: Java also required getPosition().isSpawned() (owner still in world); no C# equivalent exists.
        if (Owner.IsAlreadyDead || !_isStart) return;
        for (var i = 0; i < 5; i++)
        {
            var distance = Random.Shared.Next(4, 12);
            var npcId = SpawnNpcIds[Random.Shared.Next(2)];
            RandomSpawnInRange(npcId, distance);
        }
        ScheduleTask(Check, CheckIntervalMs);
    }

    private void RandomSpawnInRange(int npcId, float distance)
    {
        var direction = Random.Shared.Next(0, 200) / 100f;
        var x = MathF.Cos(MathF.PI * direction) * distance;
        var y = MathF.Sin(MathF.PI * direction) * distance;
        Spawn(npcId, Owner.Position.X + x, Owner.Position.Y + y, Owner.Position.Z);
    }
}
