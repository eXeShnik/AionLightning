// IsbariyaTheResoluteAI2 — Java ai/instance/beshmundirTemple/IsbariyaTheResoluteAI2.java. Beshmundir
// Temple boss: periodic basic skill cast plus HP-stage special events (soul spawn, adds, final nuke).
using System;
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("isbariya")]
public sealed class IsbariyaTheResoluteAI2 : AggressiveNpcAI2
{
    private const int SoulNpcId = 281645;
    private const int AddNpcId = 281660;
    private const int EliteAddNpcId = 281659;
    private const int BasicSkillBaseId = 18912; // + 0 or 1
    private const int LaunchSkillId = 18959;
    private const int FinalSkillId = 18993;

    private int _stage;
    private bool _isHome = true;
    private readonly List<(float X, float Y, float Z)> _soulLocations = new();

    public override void OnSpawned()
    {
        base.OnSpawned();
        _soulLocations.Clear();
        _soulLocations.Add((1580.5f, 1572.8f, 304.64f));
        _soulLocations.Add((1582.1f, 1571.2f, 304.64f));
        _soulLocations.Add((1583.3f, 1569.9f, 304.64f));
        _soulLocations.Add((1585.3f, 1568.1f, 304.64f));
        _soulLocations.Add((1586.4f, 1567.1f, 304.64f));
        _soulLocations.Add((1588.3f, 1566.2f, 304.64f));
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (_isHome)
        {
            _isHome = false;
            SendMsg(1500082);
            // note: Java also closed instance door 535 here (getWorldMapInstance().getDoors()) — instance
            // doors aren't exposed to scripts yet.
            StartBasicSkillTask();
        }
        CheckPercentage(Owner.HpPercentage);
    }

    public override void OnDied()
    {
        SendMsg(1500085);
        base.OnDied(); // cancels the basic-skill/special tasks
        // note: Java reopened instance door 535 here — instance doors aren't exposed to scripts yet.
    }

    public override void OnBackHome()
    {
        SendMsg(1500084);
        base.OnBackHome();
        _isHome = true;
        CancelTasks();
        // note: Java reopened instance door 535 here — instance doors aren't exposed to scripts yet.
        _stage = 0;
    }

    private void CheckPercentage(int hpPercentage)
    {
        if (hpPercentage <= 75 && _stage < 1)
        {
            _stage = 1;
            SendMsg(1400460);
            LaunchSpecial();
        }
        if (hpPercentage <= 50 && _stage < 2)
        {
            SendMsg(1500083);
            _stage = 2;
        }
        if (hpPercentage <= 25 && _stage < 3)
        {
            _stage = 3;
        }
    }

    private void StartBasicSkillTask()
    {
        ScheduleTask(() =>
        {
            if (!Owner.IsAlreadyDead) UseSkill(BasicSkillBaseId + Random.Shared.Next(2));
        }, 0, 24000);
    }

    private void LaunchSpecial()
    {
        if (Owner.IsAlreadyDead || _stage == 0) return;
        var delay = 10000;
        switch (_stage)
        {
            case 1:
                // note: Java targeted a random nearby living player (getKnownList().getKnownPlayers()) for
                // this skill cast; known-list membership isn't exposed to scripts yet (see
                // AggressiveNpcAI2.GetRandomTarget), so it fires without an explicit target.
                UseSkill(LaunchSkillId);
                SpawnSouls();
                delay = 25000;
                break;
            case 2:
                RandomSpawnNear(AddNpcId, 5);
                break;
            case 3:
                RandomSpawnNear(EliteAddNpcId, 1);
                UseSkill(FinalSkillId);
                delay = 20000;
                break;
        }
        ScheduleTask(LaunchSpecial, delay);
    }

    private void SpawnSouls()
    {
        var points = new List<(float X, float Y, float Z)>(_soulLocations);
        var count = Random.Shared.Next(3, 7);
        for (var i = 0; i < count && points.Count > 0; i++)
        {
            var index = Random.Shared.Next(points.Count);
            var point = points[index];
            points.RemoveAt(index);
            Spawn(SoulNpcId, point.X, point.Y, point.Z, 18);
        }
    }

    private void RandomSpawnNear(int npcId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var direction = Random.Shared.Next(0, 200) / 100f;
            var x = MathF.Cos(MathF.PI * direction) * 5f;
            var y = MathF.Sin(MathF.PI * direction) * 5f;
            Spawn(npcId, Owner.Position.X + x, Owner.Position.Y + y, Owner.Position.Z, (byte)Owner.Position.Heading);
        }
    }
}
