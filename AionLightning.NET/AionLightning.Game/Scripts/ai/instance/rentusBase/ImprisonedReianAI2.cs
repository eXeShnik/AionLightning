// ImprisonedReianAI2 — Java ai/instance/rentusBase/ImprisonedReianAI2.java. Escort/rescue npc: shouts
// as a player approaches, then "saves" itself once close enough.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("imprisoned_reian")]
public sealed class ImprisonedReianAI2 : GeneralNpcAI2
{
    private bool _isAsked;
    private bool _isSaved;

    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java captured this NPC's walker route id from the spawn template and resolved a
        // WalkerTemplate (DataManager.WALKER_DATA) to know its route-step count — walker templates aren't
        // exposed to scripts yet.
    }

    public override void OnMoveArrived()
    {
        base.OnMoveArrived();
        // note: Java stopped the walker route and deleted itself (AI2Actions.deleteOwner) once within 4
        // steps of the route's end — walker/move-controller state and NPC delete aren't exposed yet.
    }

    public override void OnCreatureMoved(Creature creature)
    {
        if (creature is not Player player) return;
        float distance = Owner.Position.DistanceTo(player.Position);
        if (distance <= 21 && !_isAsked)
        {
            _isAsked = true;
            switch (System.Random.Shared.Next(1, 11))
            {
                case 1: SendMsg(390563); break;
                case 2: SendMsg(390567); break;
            }
        }
        if (distance <= 6 && !_isSaved)
        {
            _isSaved = true;
            // note: Java restored the NPC's walker route here (WalkManager.startWalking) and broadcast a
            // START_EMOTE2 packet — neither is exposed to scripts yet.
            switch (System.Random.Shared.Next(1, 11))
            {
                case 1: SendMsg(342410); break;
                case 2: SendMsg(342411); break;
            }
        }
    }
}
