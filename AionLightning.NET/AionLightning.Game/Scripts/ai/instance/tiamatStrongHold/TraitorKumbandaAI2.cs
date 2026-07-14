// TraitorKumbandaAI2 — Java ai/instance/tiamatStrongHold/TraitorKumbandaAI2.java. Tiamat
// Stronghold boss: rare time-accelerator/ghost add spawns on attack, plus a 5%-HP self-buff.
using System;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("traitorkumbanda")]
public sealed class TraitorKumbandaAI2 : AggressiveNpcAI2
{
    private bool _isFinalBuff;

    public override void OnAttack(Creature attacker)
    {
        base.OnAttack(attacker);
        if (System.Random.Shared.Next(1, 101) < 5)
        {
            SpawnTimeAccelerator();
            SpawnKumbandaGhost();
        }
        if (!_isFinalBuff && Owner.HpPercentage <= 5)
        {
            _isFinalBuff = true;
            UseSkill(20942);
        }
    }

    private void SpawnTimeAccelerator()
    {
        if (GetNpc(283086) is null)
        {
            UseSkill(20726);
            Spawn(283086, Owner.Position.X, Owner.Position.Y, Owner.Position.Z);
            RndSpawn(283086, 6);
        }
    }

    private void SpawnKumbandaGhost()
    {
        if (GetNpc(283085) is null && Owner.HpPercentage <= 50)
        {
            Spawn(283085, Owner.Position.X, Owner.Position.Y, Owner.Position.Z);
        }
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java bulk-deleted every 283086/283088 add NPC from the current instance channel here —
        // enumerating an instance's NPCs by id and removing them isn't exposed to scripts yet.
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        _isFinalBuff = false;
    }

    private void RndSpawn(int npcId, int count)
    {
        for (int i = 0; i < count; i++)
        {
            RndSpawnInRange(npcId, System.Random.Shared.Next(10, 21));
        }
    }

    private void RndSpawnInRange(int npcId, int dist)
    {
        float direction = System.Random.Shared.Next(0, 200) / 100f;
        float x1 = (float)(Math.Cos(Math.PI * direction) * dist);
        float y1 = (float)(Math.Sin(Math.PI * direction) * dist);
        Spawn(npcId, Owner.Position.X + x1, Owner.Position.Y + y1, Owner.Position.Z);
    }
}
