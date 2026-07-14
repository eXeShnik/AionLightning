// BrigadeGeneralVashartiAI2 — Java ai/instance/rentusBase/BrigadeGeneralVashartiAI2.java. Rentus Base
// boss: HP-breakpoint "air phase" that spawns phase-specific helper npcs plus a periodic flame-buff
// event, gated by instance doors.
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("brigade_general_vasharti")]
public sealed class BrigadeGeneralVashartiAI2 : AggressiveNpcAI2
{
    private bool _isHome = true;
    private readonly List<int> _percents = new();

    public override void OnSpawned()
    {
        base.OnSpawned();
        AddPercent();
        SendMsg(1500405);
        // note: Java also seeded blue/red "flame smash" spawn-point lists here; the spawn-gating logic
        // (avoiding duplicate spawns via WorldMapInstance.getNpcs) has no C# equivalent yet.
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (_isHome)
        {
            _isHome = false;
            // note: Java opened instance door 70 and started a repeating flame-buff event (random
            // ice/fire add spawn + buff-skill casts) here — instance doors aren't exposed to scripts yet.
        }
        CheckPercentage(Owner.HpPercentage);
    }

    private void CheckPercentage(int hpPercentage)
    {
        foreach (var percent in _percents)
        {
            if (hpPercentage <= percent)
            {
                _percents.Remove(percent);
                UseSkill(20532);
                // note: Java also stopped the attack emote and switched to a walker route here —
                // EmoteManager/WalkManager/PacketSendUtility have no equivalent at the script layer.
                ScheduleTask(() => StartAirPhase(percent), 4000);
                break;
            }
        }
    }

    private void StartAirPhase(int percent)
    {
        if (Owner.IsAlreadyDead) return;
        UseSkill(20534);
        var (npcId1, npcId2) = percent switch
        {
            80 => (283010, 283002),
            70 => (283011, 283003),
            50 => (283011, 283004),
            40 => (283012, 283004),
            25 => (283012, 283006),
            _  => (0, 0),
        };
        Spawn(npcId2, 188.16568f, 414.03534f, 260.75488f);
        Spawn(npcId1, 188.33f, 414.61f, 260.61f, 244);
        var buffNpc = Spawn(283007, 188.33f, 414.61f, 260.61f);
        ScheduleTask(() =>
        {
            if (buffNpc is { IsAlreadyDead: false })
            {
                UseSkill(20538);
                // note: Java deleted buffNpc 4s later (Controller.onDelete) — scripted NPC delete isn't
                // exposed to scripts yet.
            }
        }, 1000);
        // note: after 40s Java either resumed combat or re-targeted the most-hated attacker (AggroList) —
        // aggro tracking stays owned by NpcAiService.
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 80, 70, 50, 40, 25 });
    }

    public override void OnDespawned()
    {
        _percents.Clear();
        CancelTasks();
        base.OnDespawned();
    }

    public override void OnBackHome()
    {
        _isHome = true;
        AddPercent();
        CancelTasks();
        base.OnBackHome();
        // note: Java also reopened instance door 70 here.
    }

    public override void OnDied()
    {
        _percents.Clear();
        SendMsg(1500410);
        base.OnDied();
        // note: Java also reopened instance door 70 here.
    }
}
