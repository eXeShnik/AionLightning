// LegendaryRaidAI2 — Java ai/events/LegendaryRaidAI2.java. Weekly legendary-raid bosses: only spawn
// (and only despawn) inside a specific day/hour window per boss variant.
using System;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("LegendaryRaid")]
public sealed class LegendaryRaidAI2 : AggressiveNpcAI2
{
    public override void OnAttack(Creature creature) => base.OnAttack(creature);

    public override void OnSpawned()
    {
        var now = DateTime.Now;
        int isoDay = IsoDayOfWeek(now);
        switch (Owner.Template.NpcId)
        {
            case 281810: // Omega
                if (isoDay == 1 && now.Hour == 21) base.OnSpawned();
                break;
            case 281811: // Ragnarok
                if (isoDay == 1 && now.Hour == 19) base.OnSpawned();
                break;
        }
        // note: Java despawned itself (getController().onDelete()) outside the window when not already
        // dead — NPC self-despawn isn't exposed to the script layer.
    }

    public override void OnDespawned()
    {
        var now = DateTime.Now;
        int isoDay = IsoDayOfWeek(now);
        switch (Owner.Template.NpcId)
        {
            case 281810: // Omega
                if (isoDay == 1 && now.Hour >= 22) base.OnDespawned();
                break;
            case 281811: // Ragnarok
                if (isoDay == 1 && now.Hour == 20) base.OnDespawned();
                break;
        }
    }

    /// <summary>Joda-time <c>DateTime.getDayOfWeek()</c> numbering: Monday=1 .. Sunday=7.</summary>
    private static int IsoDayOfWeek(DateTime now) => now.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)now.DayOfWeek;
}
