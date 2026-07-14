// IllusionMasterSharikAI2 — Java ai/instance/raksang/IllusionMasterSharikAI2.java. Raksang boss:
// intro shout on player approach, an 80%-HP mirror-image add spawn, and a repeating phase skill cycle.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("illusion_maseter_sharik")]
public sealed class IllusionMasterSharikAI2 : AggressiveNpcAI2
{
    private bool _startedEvent;
    private bool _started80PercentEvent;
    private int _position = 1;
    private int _percent = 100;

    public override void OnSpawned()
    {
        base.OnSpawned();
        var p = getOwner().Position;
        _position = p.X == 738.065f && p.Y == 311.606f ? 1 : 2;
    }

    public override void OnCreatureMoved(Creature creature)
    {
        if (creature is not Player player) return;
        if (getOwner().Position.DistanceTo(player.Position) > 30) return;
        if (_startedEvent) return;
        _startedEvent = true;
        // note: Java shouted 1401112 via NpcShoutsService here; NPC shouts aren't exposed to scripts yet.
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckPercentage(getOwner().HpPercentage);
    }

    private void CheckPercentage(int hpPercentage)
    {
        _percent = hpPercentage;
        if (hpPercentage > 80 || _started80PercentEvent) return;
        _started80PercentEvent = true;
        // note: Java shouted 1401136 via NpcShoutsService here; NPC shouts aren't exposed to scripts yet.
        if (_position == 1)
            Spawn(730446, 738.766f, 317.482f, 911.897f, 5);
        else
            Spawn(730447, 735.909f, 265.696f, 911.897f, unchecked((byte)278));
        StartPhaseTask();
    }

    private void StartPhaseTask()
    {
        ScheduleTask(() =>
        {
            if (getOwner().IsAlreadyDead)
            {
                CancelTasks();
                return;
            }
            // note: Java shouted 1401114 via NpcShoutsService here; NPC shouts aren't exposed to scripts
            // yet.
            UseSkill(19981, 46);
            ScheduleTask(() =>
            {
                if (getOwner().IsAlreadyDead) return;
                UseSkill(19901, 44);
                if (_percent <= 50)
                {
                    ScheduleTask(() =>
                    {
                        if (!getOwner().IsAlreadyDead)
                            UseSkill(19903, 44);
                    }, 13000);
                }
            }, 3000);
        }, 3000, 40000);
    }

    public override void OnBackHome()
    {
        DespawnMirrors();
        CancelTasks();
        base.OnBackHome();
        // note: Java shouted 1401137 via NpcShoutsService here; NPC shouts aren't exposed to scripts yet.
        if (_position == 1)
            Spawn(217425, 736.21704f, 270.8546f, 910.678f, 53);
        else
            Spawn(217425, 738.065f, 311.606f, 910.678f, 53);
        // note: Java deleted itself via AI2Actions.deleteOwner; no scripted despawn API exists yet.
    }

    private void DespawnMirrors()
    {
        // note: Java deleted every live 730446/730447 mirror via WorldMapInstance.getNpcs(id) +
        // getController().onDelete(); bulk npc-id lookup and scripted despawn aren't exposed to scripts
        // yet.
    }

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }

    public override void OnDied()
    {
        DespawnMirrors();
        CancelTasks();
        // note: Java opened instance doors 294 and 295 here; door control isn't exposed to scripts yet.
        base.OnDied();
    }
}
