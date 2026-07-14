// CaptainLediarAI2 — Java ai/instance/shugoImperialTomb/CaptainLediarAI2.java. Shugo Imperial Tomb
// boss: spawns helper adds at 75% hp; once its walk route finishes it adds hate toward the tomb's
// defense towers.
using System;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("captain_lediar")]
// 219531
public sealed class CaptainLediarAI2 : AggressiveNpcAI2
{
    private bool _spawnedHelpers;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckPercentage(Owner.HpPercentage);
    }

    private void CheckPercentage(int hpPercentage)
    {
        if (hpPercentage == 75 && !_spawnedHelpers)
        {
            _spawnedHelpers = true;
            SpawnHelpers();
        }
    }

    private void SpawnHelpers()
    {
        if (Owner.IsAlreadyDead) return;
        for (int i = 0; i < 4; i++)
            RndSpawnInRange(219509, 2f);
        // note: Java tracked the spawned objectIds to delete them on death (World.findVisibleObject +
        // onDelete); NPC deletion by objectId isn't exposed to scripts yet, so helpers aren't cleaned up.
    }

    private void RndSpawnInRange(int npcId, float distance)
    {
        double direction = Random.Shared.Next(0, 200) / 100.0;
        float x = (float)(Math.Cos(Math.PI * direction) * distance);
        float y = (float)(Math.Sin(Math.PI * direction) * distance);
        Spawn(npcId, Owner.Position.X + x, Owner.Position.Y + y, Owner.Position.Z);
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: see SpawnHelpers — helper cleanup isn't wired.
    }

    // note: Java also overrode modifyOwnerDamage (clamp to 1) and canThink (locked out until its walk
    // route finished) — neither hook exists on NpcAi2 yet.

    public override void OnMoveArrived()
    {
        base.OnMoveArrived();
        // note: Java checked whether this was the walker route's final point (DataManager.WALKER_DATA +
        // getMoveController().getCurrentPoint()) and, once there, stopped walking (WalkManager),
        // re-enabled thinking, and added hate toward the tomb's defense towers (831251/831250/831305)
        // via EmoteManager.emoteStopAttacking + AggroList.addHate. Walker-route/move-controller state and
        // EmoteManager aren't exposed to scripts yet.
    }
}
