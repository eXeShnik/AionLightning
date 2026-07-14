using System;
// PashidLabUnitDroneAI2 — Java ai/instance/danuarMysticarium/PashidLabUnitDroneAI2.java. Pashid lab
// drone: on being hit, may call in a Sentinel helper (spawn + shout) if one isn't already up.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("pashid_lab_unit_drone")]
public sealed class PashidLabUnitDroneAI2 : AggressiveNpcAI2
{
    private const int SentinelNpcId = 230077;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (Random.Shared.Next(1, 101) < 50) SpawnHelper();
        ShoutEvent();
    }

    private void SpawnHelper()
    {
        if (GetNpc(SentinelNpcId) is not null) return;
        SpawnAt();
        SendMsg(342822);
        UseSkill(19498);
    }

    private void SpawnAt()
    {
        float direction = Random.Shared.Next(0, 200) / 100f;
        float x1 = Owner.Position.X + MathF.Cos(MathF.PI * direction) * 5;
        float y1 = Owner.Position.Y + MathF.Sin(MathF.PI * direction) * 5;
        float x2 = Owner.Position.X + MathF.Cos(MathF.PI * direction) * 2;
        float y2 = Owner.Position.Y + MathF.Sin(MathF.PI * direction) * 2;
        Spawn(SentinelNpcId, x1, y1, Owner.Position.Z);
        Spawn(SentinelNpcId, x2, y2, Owner.Position.Z);
        // note: Java also aggroed each freshly spawned sentinel onto every known-list player
        // (Npc.setTarget/AIState.WALKING/MoveController.moveToTargetObject + an SM_EMOTION
        // broadcast); controlling another NPC's AI state isn't exposed to the script layer yet.
    }

    private void ShoutEvent()
    {
        if (GetNpc(SentinelNpcId) is not null) return;
        ScheduleTask(() => SendMsg(342819), 1000);
        ScheduleTask(() => SendMsg(342820), 10000);
        ScheduleTask(() => SendMsg(342821), 15000);
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        DespawnHelper();
    }

    public override void OnDied()
    {
        base.OnDied();
        DespawnHelper();
    }

    private void DespawnHelper()
    {
        // note: Java looked up the Sentinel NPC in the owner's known-list scope and deleted it
        // (Npc.getController().onDelete()); deleting another NPC isn't exposed to the script layer
        // yet.
    }
}
