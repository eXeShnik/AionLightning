using System;
using System.Collections.Generic;
// ModorCursedAI2 — Java ai/instance/danuarReliquary/ModorCursedAI2.java. Danuar Reliquary boss:
// once aggroed, cycles random self-cast skills every 30s, teleports away at 75% HP, and turns
// briefly invincible (spawning clone helpers) at 50% HP.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("modorcursed")]
public sealed class ModorCursedAI2 : AggressiveNpcAI2
{
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
        CheckPercentage(Owner.HpPercentage);
        if (_isHome)
        {
            _isHome = false;
            SendMsg(1500740);
            StartSkillTask();
        }
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 75, 50, 25 });
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
        _percents.Clear();
    }

    public override void OnBackHome()
    {
        AddPercent();
        base.OnBackHome();
        _isHome = true;
    }

    private void CheckPercentage(int hpPercentage)
    {
        foreach (int percent in _percents)
        {
            if (hpPercentage <= percent)
            {
                if (percent == 75) Teleport();
                if (percent == 50) StartInvincibleTask();
                _percents.Remove(percent);
                break;
            }
        }
    }

    private void StartSkillTask() => ScheduleTask(() =>
    {
        if (Owner.IsAlreadyDead) return;
        ChooseRandomEvent();
    }, 4000, 30000);

    private void ChooseRandomEvent()
    {
        int rand = Random.Shared.Next(0, 6);
        if (rand == 0) Anger();
        if (rand == 1) Sphere();
        if (rand == 2) Revenge();
        if (rand == 3) Wrath();
        if (rand == 4) Snow();
        else Storm();
    }

    private void Snow() => UseSkill(21367, 55);
    private void Wrath() => UseSkill(21229, 55);
    private void Revenge() => UseSkill(21175, 55);
    private void Sphere() => UseSkill(21174, 55);
    private void Anger() => UseSkill(21171, 55);
    private void Storm() => UseSkill(21173, 55);

    private void Teleport()
    {
        UseSkill(21165, 65);
        SendMsg(1500741);
        ScheduleTask(() =>
        {
            Owner.Position = Owner.Position with { X = 255.52051f, Y = 293.178f, Z = 253.8094f, Heading = 30 };
            // note: Java also broadcast an SM_FORCED_MOVE so nearby clients snap to the new position;
            // PacketSendUtility isn't wired at the script layer yet.
            Spawn(284664, 266.879f, 247.496f, 242.03f, 45);
            Spawn(284380, 246.349f, 247.481f, 242.01f, 15);
            Spawn(284381, 257.405f, 243.156f, 241.91f, 31);
        }, 2000);
    }

    private void StartInvincibleTask()
    {
        UseSkill(21165, 65);
        SendMsg(1500742);
        // note: Java also disabled canThink() (no C# equivalent) and stopped the current attack
        // (EmoteManager.emoteStopAttacking); neither is exposed to the script layer yet.
        ScheduleTask(() =>
        {
            UseSkill(21167, 65);
            Owner.Position = Owner.Position with { X = 256.4457f, Y = 257.6867f, Z = 253.0f, Heading = 30 };
            // note: Java also broadcast an SM_FORCED_MOVE; PacketSendUtility isn't wired at the script
            // layer yet.
            Spawn(284383, 284.50403f, 262.8162f, 248.77342f, 63);
            Spawn(284384, 271.2224f, 230.51619f, 250.94952f, 36);
            Spawn(284384, 240.22404f, 235.21863f, 251.1519f, 20);
            Spawn(284384, 232.55373f, 263.82864f, 248.65227f, 115);
            Spawn(284384, 255.52051f, 293.178f, 253.8094f, 90);
        }, 2000);
    }
}
