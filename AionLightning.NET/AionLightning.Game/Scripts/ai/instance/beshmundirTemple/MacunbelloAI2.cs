// MacunbelloAI2 — Java ai/instance/beshmundirTemple/MacunbelloAI2.java. Beshmundir Temple boss:
// two HP-breakpoint waves of "Macunbello's Right Hand" adds.
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("macunbello")]
public sealed class MacunbelloAI2 : AggressiveNpcAI2
{
    private const int RightHandNpcId = 281698;

    private bool _isHome = true;
    private bool _wave1;
    private bool _wave2;
    private readonly List<Npc> _waveNpcs = new();

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (_isHome)
        {
            _isHome = false;
            SendMsg(1500060);
        }
        CheckPercentage(Owner.HpPercentage);
    }

    public override void OnDied()
    {
        base.OnDied(); // cancels the scheduled wave spawns
        // note: Java despawned every tracked wave add via npc.getController().onDelete(); no scripted
        // despawn API exists yet, so the tracked list is cleared without removing the adds from the world.
        _waveNpcs.Clear();
        SendMsg(1500063);
    }

    public override void OnBackHome()
    {
        CancelTasks();
        // note: same untracked-despawn gap as OnDied.
        _waveNpcs.Clear();
        _isHome = true;
        base.OnBackHome();
    }

    private void CheckPercentage(int hpPercentage)
    {
        if (hpPercentage <= 70 && !_wave1)
        {
            _wave1 = true;
            SendMsg(1500061);
            ScheduleTask(SpawnWave, 4000);
            return;
        }
        if (hpPercentage <= 30 && !_wave2)
        {
            _wave2 = true;
            SendMsg(1500061);
            ScheduleTask(SpawnWave, 4000);
        }
    }

    private void SpawnWave()
    {
        if (Owner.IsAlreadyDead) return;
        AddWaveNpc(Spawn(RightHandNpcId, 955.97986f, 160.24693f, 241.77303f, 108));
        AddWaveNpc(Spawn(RightHandNpcId, 1003.958f, 159.76878f, 241.77016f, 30));
        AddWaveNpc(Spawn(RightHandNpcId, 1007.1438f, 109.738075f, 242.7066f, 30));
        AddWaveNpc(Spawn(RightHandNpcId, 952.05365f, 109.55048f, 242.7103f, 30));
        SendMsg(1500062);
    }

    private void AddWaveNpc(Npc? npc)
    {
        if (npc is not null) _waveNpcs.Add(npc);
    }
}
