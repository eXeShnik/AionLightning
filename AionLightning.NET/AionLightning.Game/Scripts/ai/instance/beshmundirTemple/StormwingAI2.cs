// StormwingAI2 — Java ai/instance/beshmundirTemple/StormwingAI2.java. Beshmundir Temple boss:
// HP-breakpoint skill phases plus a recurring Root/Sharp Twister and Voltaic Storm add chain.
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("stormwing")]
public sealed class StormwingAI2 : AggressiveNpcAI2
{
    private const int StormwingNpcId = 216183;
    private const int RootTwisterNpcId = 281794;
    private const int SharpTwisterNpcId = 281796;
    private const int VoltaicStormNpcId = 281798;
    private const int ThreshingWindSkillId = 18613;
    private const int DistantWindSkillId = 18612;
    private const int VoltaicStormSkillId = 18617;
    private const int ThresholdSkillAId = 18615;
    private const int ThresholdSkillBId = 18616;

    private bool _isHome = true;
    private readonly List<int> _percents = new();

    public override void OnSpawned()
    {
        base.OnSpawned();
        AddPercent();
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (_isHome)
        {
            _isHome = false;
            SendMsg(1500086);
            ScheduleRespawns();
        }
        CheckPercentage(Owner.HpPercentage);
    }

    public override void OnDied()
    {
        base.OnDied(); // cancels the scheduled twister/storm spawns
        _percents.Clear();
        SendMsg(1500092);
        // note: Java despawned every already-spawned Root/Sharp Twister and Voltaic Storm add via
        // npc.getController().onDelete(); no scripted despawn API exists yet.
    }

    public override void OnBackHome()
    {
        AddPercent();
        CancelTasks();
        base.OnBackHome();
        _isHome = true;
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 90, 80, 75, 50, 10 });
    }

    private void CheckPercentage(int hpPercentage)
    {
        foreach (var percent in _percents)
        {
            if (hpPercentage > percent) continue;
            switch (percent)
            {
                case 90:
                case 50:
                case 10:
                    UseSkill(ThresholdSkillBId);
                    break;
                case 80:
                case 75:
                    UseSkill(ThresholdSkillAId);
                    break;
            }
            _percents.Remove(percent);
            break;
        }
    }

    private void SpawnRootTwister()
    {
        var stormwing = GetNpc(StormwingNpcId);
        if (stormwing is null || stormwing.IsAlreadyDead) return;
        if (GetNpc(RootTwisterNpcId) is null)
        {
            Spawn(RootTwisterNpcId, 539.695f, 1363.87f, 223.529f, 8);
            Spawn(RootTwisterNpcId, 547.128f, 1354.55f, 223.529f, 16);
            Spawn(RootTwisterNpcId, 557.449f, 1350.75f, 223.529f, 28);
            UseSkill(ThreshingWindSkillId);
            SendMsg(1500088);
        }
        ScheduleRespawns();
    }

    private void SpawnSharpTwister()
    {
        var stormwing = GetNpc(StormwingNpcId);
        if (stormwing is null || stormwing.IsAlreadyDead) return;
        if (GetNpc(SharpTwisterNpcId) is null)
        {
            Spawn(SharpTwisterNpcId, 544.181f, 1380.58f, 223.529f, 108);
            Spawn(SharpTwisterNpcId, 572.159f, 1358.19f, 223.529f, 48);
            UseSkill(DistantWindSkillId);
            SendMsg(1500087);
        }
        ScheduleRespawns();
    }

    private void SpawnVoltaicStorm()
    {
        var stormwing = GetNpc(StormwingNpcId);
        if (stormwing is null || stormwing.IsAlreadyDead) return;
        if (GetNpc(VoltaicStormNpcId) is null)
        {
            Spawn(VoltaicStormNpcId, 553.692f, 1330.63f, 2230529f, 35); // Z literal mirrors the Java source
            Spawn(VoltaicStormNpcId, 525.885f, 1348.14f, 223.529f, 13);
            UseSkill(VoltaicStormSkillId);
            SendMsg(1500089);
        }
        ScheduleRespawns();
    }

    private void ScheduleRespawns()
    {
        ScheduleTask(SpawnVoltaicStorm, 150000); // Voltaic Storm spawns every 2 minutes.
        ScheduleTask(SpawnSharpTwister, 300000); // Sharp Twister spawns every 5 minutes.
        ScheduleTask(SpawnRootTwister, 600000);  // Root Twister spawns every 10 minutes.
    }
}
