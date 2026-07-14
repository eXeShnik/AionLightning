// CalindiFlamelordAI2 — Java ai/instance/dragonLordsRefuge/CalindiFlamelordAI2.java. Calindi boss:
// periodic hallucinatory-victory add wave, chance-based blaze-engraving trap, and a final self-buff
// below 12% HP.
using System;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai.DragonLordsRefuge;

[AiName("calindiflamelord60")]
// 219359
public sealed class CalindiFlamelordAI2 : AggressiveNpcAI2
{
    private bool _isHome = true;
    private bool _isFinalBuff;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (_isHome)
        {
            _isHome = false;
            StartSkillTask();
        }
        if (!_isFinalBuff)
        {
            BlazeEngraving();
            if (getOwner().HpPercentage <= 12)
            {
                _isFinalBuff = true;
                CancelTasks();
                UseSkill(20915);
            }
        }
    }

    private void StartSkillTask()
    {
        ScheduleTask(() =>
        {
            // note: Java guarded each tick with isAlreadyDead()/cancelTask(); CancelTasks() already stops
            // this loop from the base OnDied() hook, so the guard is redundant here.
            StartHallucinatoryVictoryEvent();
        }, 5000, 80000);
    }

    private void StartHallucinatoryVictoryEvent()
    {
        // note: Java first checked that adds 730695/730696 weren't already spawned (WorldMapInstance.
        // getNpc); that existence probe isn't exposed beyond GetNpc's owner-scope lookup, so this always
        // fires. Also applied two self-buffs directly via SkillEngine.applyEffectDirectly, which has no
        // scripted equivalent yet.
        UseSkill(20911);
        Spawn(730695, 482.21f, 458.06f, 427.42f, 98);
        Spawn(730696, 482.21f, 571.16f, 427.42f, 22);
        RndSpawn(283132, 10);
    }

    private void BlazeEngraving()
    {
        if (Random.Shared.Next(0, 101) < 2)
        {
            // note: Java also checked WorldMapInstance.getNpc(283130) == null and cast skill 20913 without
            // animation before spawning the trap; that existence probe and no-animation skill cast aren't
            // exposed to scripts yet.
            var target = GetRandomTarget();
            if (target is null) return;
            Spawn(283130, target.Position.X, target.Position.Y, target.Position.Z, 0);
        }
    }

    private void RndSpawn(int npcId, int count)
    {
        for (int i = 0; i < count; i++)
        {
            double direction = Math.PI * (Random.Shared.Next(0, 200) / 100.0);
            int range = Random.Shared.Next(5, 21);
            float x = getOwner().Position.X + (float)(Math.Cos(direction) * range);
            float y = getOwner().Position.Y + (float)(Math.Sin(direction) * range);
            Spawn(npcId, x, y, getOwner().Position.Z, (byte)getOwner().Position.Heading);
        }
    }

    public override void OnDied()
    {
        base.OnDied();
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        _isFinalBuff = false;
        _isHome = true;
    }
}
