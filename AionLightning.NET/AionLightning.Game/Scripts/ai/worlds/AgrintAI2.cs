// AgrintAI2 — Java ai/worlds/AgrintAI2.java. Open-world elite: shouts on spawn, spawns extra helpers
// past 50% HP, and spawns loot chests on death.
using System;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("agrint")]
public sealed class AgrintAI2 : AggressiveNpcAI2
{
    private bool _isSpawned;

    public override void OnSpawned()
    {
        base.OnSpawned();
        int msg = Owner.Template.NpcId switch
        {
            218862 or 218850 => 1401246,
            218863 or 218851 => 1401247,
            218864 or 218852 => 1401248,
            218865 or 218853 => 1401249,
            _ => 0,
        };
        SendMsg(msg); // note: Java also passed a 2000ms shout delay; SendMsg has no delay parameter yet.
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckPercentage(Owner.HpPercentage);
    }

    private void CheckPercentage(int hpPercentage)
    {
        if (hpPercentage > 50) return;
        lock (this)
        {
            if (_isSpawned) return;
            _isSpawned = true;
        }
        int npcId = Owner.Template.NpcId switch
        {
            218850 or 218851 or 218852 or 218853 => Owner.Template.NpcId + 320,
            218862 or 218863 or 218864 or 218865 => Owner.Template.NpcId + 308,
            _ => 0,
        };
        if (npcId == 0) return;
        for (int i = 0; i < 5; i++)
            RndSpawnInRange(npcId, Random.Shared.Next(1, 3));
    }

    private void RndSpawnInRange(int npcId, float distance)
    {
        float direction = Random.Shared.Next(0, 200) / 100f;
        float x1 = MathF.Cos(MathF.PI * direction) * distance;
        float y1 = MathF.Sin(MathF.PI * direction) * distance;
        Spawn(npcId, Owner.Position.X + x1, Owner.Position.Y + y1, Owner.Position.Z);
    }

    public override void OnBackHome()
    {
        _isSpawned = false;
        base.OnBackHome();
    }

    private void SpawnChests(int npcId)
    {
        for (int i = 0; i < 6; i++)
            RndSpawnInRange(npcId, Random.Shared.Next(1, 7));
    }

    public override void OnDied()
    {
        switch (Owner.Template.NpcId)
        {
            case 218850: SpawnChests(218874); break;
            case 218851: SpawnChests(218876); break;
            case 218852: SpawnChests(218878); break;
            case 218853: SpawnChests(218880); break;
            case 218862: SpawnChests(218882); break;
            case 218863: SpawnChests(218884); break;
            case 218864: SpawnChests(218886); break;
            case 218865: SpawnChests(218888); break;
        }
        base.OnDied();
        // note: Java also overrode modifyDamage/modifyOwnerDamage to clamp damage to 1; no
        // damage-modification hook exists on NpcAi2 yet.
    }
}
