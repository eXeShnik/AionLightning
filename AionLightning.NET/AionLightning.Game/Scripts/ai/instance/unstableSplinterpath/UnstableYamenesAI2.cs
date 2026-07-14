using System.Linq;
using System.Collections.Generic;
using System;
// UnstableYamenesAI2 — Java ai/instance/unstableSplinterpath/UnstableYamenesAI2.java. Final boss:
// enrages after 10min, and periodically spawns a portal + wave-of-pain adds.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("unstableyamennes")]
public sealed class UnstableYamenesAI2 : AggressiveNpcAI2
{
    private bool _top;
    private readonly List<int> _percents = new();
    private bool _isStart;

    public override void OnSpawned()
    {
        AddPercent();
        _top = true;
        SendMsg(1400732);
        base.OnSpawned();
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (!_isStart)
        {
            _isStart = true;
            StartTasks();
        }
    }

    private void StartTasks()
    {
        ScheduleTask(() =>
        {
            if (!Owner.IsAlreadyDead)
                UseSkill(19098);
        }, 600000);

        ScheduleTask(() =>
        {
            if (Owner.IsAlreadyDead)
            {
                CancelTasks();
            }
            else
            {
                SpawnPortal();
                ScheduleTask(() =>
                {
                    DeleteNpcs(219586);
                    // note: Java also stopped its attack emote (EmoteManager) before casting; not exposed.
                    UseSkill(19282);
                    Spawn(219586, Owner.Position.X + 10, Owner.Position.Y - 10, Owner.Position.Z);
                    Spawn(219586, Owner.Position.X - 10, Owner.Position.Y + 10, Owner.Position.Z);
                    Spawn(219586, Owner.Position.X + 10, Owner.Position.Y + 10, Owner.Position.Z);
                    // note: Java also reset the owner's attacked-count (clearAttackedCount) here; no
                    // scripted equivalent exists yet.
                    SendMsg(1400729);
                }, 3000);
            }
        }, 60000, 60000);
    }

    private void SpawnPortal()
    {
        var portalA = GetNpc(219580);
        var portalB = GetNpc(219579);
        var portalC = GetNpc(219567);
        if (portalA is null && portalB is null && portalC is null)
        {
            if (!_top)
            {
                SendMsg(1400637);
                Spawn(219567, 288.10f, 741.95f, 216.81f, 3);
                Spawn(219579, 375.05f, 750.67f, 216.82f, 59);
                Spawn(219580, 341.33f, 699.38f, 216.86f, 59);
                _top = true;
            }
            else
            {
                SendMsg(1400637);
                Spawn(219567, 303.69f, 736.35f, 198.7f, 0);
                Spawn(219579, 335.19f, 708.92f, 198.9f, 35);
                Spawn(219580, 360.23f, 741.07f, 198.7f, 0);
                _top = false;
            }
        }
    }

    private void DeleteNpcs(int npcId)
    {
        // note: Java deleted every live npc with this id via WorldMapInstance.getNpcs(id) +
        // getController().onDelete(); bulk npc-id lookup and scripted despawn aren't exposed to scripts
        // yet.
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.Add(100);
    }

    public override void OnBackHome()
    {
        AddPercent();
        _top = true;
        CancelTasks();
        _isStart = false;
        base.OnBackHome();
    }

    public override void OnDespawned()
    {
        _percents.Clear();
        CancelTasks();
        DeleteNpcs(219586);
        base.OnDespawned();
    }

    public override void OnDied()
    {
        _percents.Clear();
        CancelTasks();
        DeleteNpcs(219586);
        base.OnDied();
    }
}
