using System.Linq;
using System.Collections.Generic;
using System;
// UnstableKaluvaSpawnAI2 — Java ai/instance/unstableSplinterpath/UnstableKaluvaSpawnAI2.java.
// Kaluva's egg add: hatches into one of 4 add formations 28s after a player gets close (when
// Kaluva's debuff ends), then cleans up Kaluva's debuff.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("unstablekaluvaspawn")]
public sealed class UnstableKaluvaSpawnAI2 : NpcAi2
{
    private bool _hatchScheduled;

    public override void OnDied()
    {
        base.OnDied();
        CheckKaluva();
    }

    public override void OnCreatureSee(Creature creature) => CheckDistance(creature);

    public override void OnCreatureMoved(Creature creature) => CheckDistance(creature);

    private void CheckDistance(Creature creature)
    {
        if (creature is not Npc) return;
        var kaluva = GetNpc(219553);
        if (kaluva is not null && Owner.Position.DistanceTo(kaluva.Position) <= 7 && !_hatchScheduled)
        {
            kaluva.RemoveEffectBySkillId(19152);
            ScheduleHatch();
        }
    }

    public override void OnSpawned()
    {
        base.OnSpawned();
        UseSkill(19222);
    }

    private void CheckKaluva()
    {
        var kaluva = GetNpc(219553);
        if (kaluva is not null && !kaluva.IsAlreadyDead)
            kaluva.RemoveEffectBySkillId(19152);
        // note: Java then deleted itself via AI2Actions.deleteOwner; no scripted despawn API exists yet.
    }

    private void ScheduleHatch()
    {
        _hatchScheduled = true;
        ScheduleTask(() =>
        {
            if (!Owner.IsAlreadyDead)
            {
                HatchAdds();
                CheckKaluva();
            }
        }, 28000);
    }

    private void HatchAdds() // 4 different spawn-formations; see Powerwiki for more information
    {
        var p = Owner.Position;
        byte heading = (byte)p.Heading;
        switch (Random.Shared.Next(1, 5))
        {
            case 1:
                Spawn(219572, p.X, p.Y, p.Z, heading);
                Spawn(219572, p.X, p.Y, p.Z, heading);
                break;
            case 2:
                for (int i = 0; i < 12; i++)
                    Spawn(219573, p.X, p.Y, p.Z, heading);
                break;
            case 3:
                Spawn(219574, p.X, p.Y, p.Z, heading);
                break;
            case 4:
                Spawn(219572, p.X, p.Y, p.Z, heading);
                Spawn(219573, p.X, p.Y, p.Z, heading);
                Spawn(219573, p.X, p.Y, p.Z, heading);
                Spawn(219573, p.X, p.Y, p.Z, heading);
                break;
        }
    }
}
