// TelepathyControllerAI2 — Java ai/instance/darkPoeta/TelepathyControllerAI2.java. Boss that spawns
// a random helper npc near itself once at 50% and once at 10% HP.
using System;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("telepathycontroller")]
public sealed class TelepathyControllerAI2 : AggressiveNpcAI2
{
    private bool _isStart50Event;
    private bool _isStart10Event;

    private void CheckPercentage(int hpPercentage)
    {
        if (hpPercentage <= 50)
        {
            if (!_isStart50Event)
            {
                _isStart50Event = true;
                Helper();
            }
        }
        else if (hpPercentage <= 10)
        {
            if (!_isStart10Event)
            {
                _isStart10Event = true;
                Helper();
            }
        }
    }

    public override void OnBackHome()
    {
        _isStart50Event = false;
        _isStart10Event = false;
        base.OnBackHome();
    }

    private void Helper()
    {
        // note: Java also gated this on getPosition().isSpawned(); spawned-state isn't exposed to
        // scripts, so only the death guard is checked here.
        if (Owner.IsAlreadyDead) return;

        int distance = Random.Shared.Next(7, 11);
        int npcId = Random.Shared.Next(1, 3) == 1 ? 281150 : 281334; // Anuhart Escort / Bionic Clodworm
        RndSpawnInRange(npcId, distance);
    }

    private void RndSpawnInRange(int npcId, float distance)
    {
        float direction = Random.Shared.Next(0, 200) / 100f;
        float x1 = (float)(Math.Cos(Math.PI * direction) * distance);
        float y1 = (float)(Math.Sin(Math.PI * direction) * distance);
        Spawn(npcId, Owner.Position.X + x1, Owner.Position.Y + y1, Owner.Position.Z);
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckPercentage(Owner.HpPercentage);
    }
}
