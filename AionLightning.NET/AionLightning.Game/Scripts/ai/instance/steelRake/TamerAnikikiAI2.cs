// TamerAnikikiAI2 — Java ai/instance/steelRake/TamerAnikikiAI2.java. Steel Rake walker npc that
// starts a scripted walk + key-box spawn when a player gets close, and self-buffs on spawn.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("tamer_anikiki")]
public sealed class TamerAnikikiAI2 : GeneralNpcAI2
{
    private bool _isStartedWalkEvent;

    // note: Java's canThink()/modifyDamage(int) overrides have no NpcAi2 equivalent — dropped.

    public override void OnCreatureMoved(Creature creature)
    {
        base.OnCreatureMoved(creature);
        if (Owner.Template.NpcId == 219040 && creature is Player
            && Owner.Position.DistanceTo(creature.Position) <= 10 && !_isStartedWalkEvent)
        {
            _isStartedWalkEvent = true;
            // note: Java started walker route "3004600001" (WalkManager.startWalking) and broadcast an
            // SM_EMOTION/SM_QUEST_ACTION plus two NpcShoutsService shouts; walker routes and those
            // packets aren't wired at the script layer yet.
            Spawn(700553, 611, 481, 936, 90); // Key Box
            Spawn(700553, 657, 482, 936, 60);
            Spawn(700553, 626, 540, 936, 1);
            Spawn(700553, 645, 534, 936, 75);
        }
    }

    public override void OnMoveArrived()
    {
        base.OnMoveArrived();
        // note: Java branched on getMoveController().getCurrentPoint() (walker waypoint 8/12) to play
        // an emote or stop the walker and self-delete; walker waypoint tracking isn't exposed to
        // scripts.
    }

    public override void OnSpawned()
    {
        base.OnSpawned();
        if (Owner.Template.NpcId != 219040)
        {
            ScheduleTask(() => UseSkill(18189, 20), 5000);
        }
    }
}
