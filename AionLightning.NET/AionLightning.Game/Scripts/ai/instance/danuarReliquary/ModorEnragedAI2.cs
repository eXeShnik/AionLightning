using System;
using System.Collections.Generic;
// ModorEnragedAI2 — Java ai/instance/danuarReliquary/ModorEnragedAI2.java. Danuar Reliquary boss:
// once aggroed, cycles random self-cast skills every 50s and randomly teleports every ~130s.
// note: each Java teleport also broadcast an SM_FORCED_MOVE so nearby clients snap to the new
// position; PacketSendUtility isn't wired at the script layer yet, so only the position itself moves.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("modorenraged")]
public sealed class ModorEnragedAI2 : AggressiveNpcAI2
{
    private bool _isHome = true;

    // note: Java declared a percents list here for a checkPercentage(hp<=99 -> Scream()) gate, but
    // never populated it (no addPercent() call exists in this class), so the gate never fired in the
    // original either — kept empty to preserve that behavior faithfully.
    private readonly List<int> _percents = new();

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckPercentage(Owner.HpPercentage);
        if (_isHome)
        {
            _isHome = false;
            SendMsg(1500743);
            StartSkillTask();
            StartTeleportTask();
        }
    }

    private void CheckPercentage(int hpPercentage)
    {
        foreach (int percent in _percents)
        {
            if (hpPercentage <= percent)
            {
                if (percent == 99) Scream();
                _percents.Remove(percent);
                break;
            }
        }
    }

    private void StartTeleportTask() => ScheduleTask(() =>
    {
        if (Owner.IsAlreadyDead) return;
        TeleportRandomLoc();
    }, 120000, 130000);

    private void TeleportRandomLoc()
    {
        switch (Random.Shared.Next(1, 5))
        {
            case 1: Teleport1(); break;
            case 2: Teleport2(); break;
            case 3: Teleport3(); break;
            case 4: Teleport4(); break;
        }
    }

    private void StartSkillTask() => ScheduleTask(() =>
    {
        if (Owner.IsAlreadyDead) return;
        ChooseRandomEvent();
    }, 4000, 50000);

    private void ChooseRandomEvent()
    {
        int rand = Random.Shared.Next(0, 4);
        if (rand == 0) Anger();
        if (rand == 1) Scream();
        if (rand == 2) Roar();
        else Storm();
    }

    private void Roar() => UseSkill(21269, 55);
    private void Scream() => UseSkill(21268, 55);
    private void Anger() => UseSkill(21171, 55);
    private void Storm() => UseSkill(21173, 55);

    public override void OnDespawned()
    {
        base.OnDespawned();
        CancelTasks();
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        _isHome = true;
        CancelTasks();
    }

    public override void OnDied()
    {
        base.OnDied();
        CancelTasks();
    }

    private void Teleport1()
    {
        UseSkill(21165, 65);
        SendMsg(1500741);
        ScheduleTask(() =>
        {
            Owner.Position = Owner.Position with { X = 255f, Y = 293f, Z = 253f, Heading = 22 };
            Spawn(284382, 266.879f, 247.496f, 242.03f, 45);
            Spawn(284663, 246.349f, 247.481f, 242.01f, 15);
            Spawn(284660, 257.405f, 243.156f, 241.91f, 31);
        }, 2000);
    }

    private void Teleport2()
    {
        UseSkill(21165, 65);
        SendMsg(1500741);
        ScheduleTask(() =>
        {
            Owner.Position = Owner.Position with { X = 284f, Y = 262f, Z = 248f, Heading = 22 };
            Spawn(284661, 266.879f, 247.496f, 242.03f, 45);
            Spawn(284662, 246.349f, 247.481f, 242.01f, 15);
            Spawn(284659, 257.405f, 243.156f, 241.91f, 31);
        }, 2000);
    }

    private void Teleport3()
    {
        UseSkill(21165, 65);
        SendMsg(1500741);
        ScheduleTask(() =>
        {
            Owner.Position = Owner.Position with { X = 271f, Y = 230f, Z = 251f, Heading = 22 };
            Spawn(284382, 266.879f, 247.496f, 242.03f, 45);
            Spawn(284663, 246.349f, 247.481f, 242.01f, 15);
            Spawn(284660, 257.405f, 243.156f, 241.91f, 31);
        }, 2000);
    }

    private void Teleport4()
    {
        UseSkill(21165, 65);
        SendMsg(1500741);
        ScheduleTask(() =>
        {
            Owner.Position = Owner.Position with { X = 240f, Y = 235f, Z = 251f, Heading = 22 };
            Spawn(284661, 266.879f, 247.496f, 242.03f, 45);
            Spawn(284662, 246.349f, 247.481f, 242.01f, 15);
            Spawn(284659, 257.405f, 243.156f, 241.91f, 31);
        }, 2000);
    }
}
