// BrigadeGeneralLaksyakaAI2 — Java ai/instance/tiamatStrongHold/BrigadeGeneralLaksyakaAI2.java.
// Tiamat Stronghold boss: rare skeleton-add spawn on attack, a periodic debuff event on a random
// nearby player, and a 25%-HP self-buff.
using System;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("brigadegenerallaksyaka")]
public sealed class BrigadeGeneralLaksyakaAI2 : AggressiveNpcAI2
{
    private bool _isHome = true;
    private bool _isFinalBuff;

    public override void OnAttack(Creature attacker)
    {
        base.OnAttack(attacker);
        if (System.Random.Shared.Next(101) < 3)
        {
            SpawnSummon();
        }
        if (_isHome)
        {
            _isHome = false;
            StartSkillTask();
        }
        if (!_isFinalBuff && Owner.HpPercentage <= 25)
        {
            _isFinalBuff = true;
            UseSkill(20731);
        }
    }

    private void StartSkillTask()
    {
        ScheduleTask(() =>
        {
            if (Owner.IsAlreadyDead) CancelTasks();
            else StartSkeletonEvent();
        }, 5000, 40000);
    }

    private void StartSkeletonEvent()
    {
        // note: Java targeted a random known player within 40m of the Tiamat Eye NPC (283089) and
        // applied debuff 20865 to them via SkillEngine.applyEffectDirectly. Known-player enumeration
        // and direct-effect application aren't exposed to scripts yet.
    }

    private void SpawnSummon()
    {
        if (GetNpc(283115) is null)
        {
            RndSpawn(283115, 4);
        }
    }

    private void RndSpawn(int npcId, int count)
    {
        for (int i = 0; i < count; i++)
        {
            RndSpawnInRange(npcId, 10);
        }
    }

    private void RndSpawnInRange(int npcId, int dist)
    {
        float direction = System.Random.Shared.Next(0, 200) / 100f;
        float x1 = (float)(Math.Cos(Math.PI * direction) * dist);
        float y1 = (float)(Math.Sin(Math.PI * direction) * dist);
        Spawn(npcId, Owner.Position.X + x1, Owner.Position.Y + y1, Owner.Position.Z);
    }

    public override void OnDied()
    {
        base.OnDied();
        CancelTasks();
    }

    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java flipped the owner to CreatureType.PEACE on spawn (getOwner().setNpcType) — NPC
        // type is fixed from the template at construction, no runtime setter exists yet.
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
        CancelTasks();
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        CancelTasks();
        _isFinalBuff = false;
        _isHome = true;
    }
}
