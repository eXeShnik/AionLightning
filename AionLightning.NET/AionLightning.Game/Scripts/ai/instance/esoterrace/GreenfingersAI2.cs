// GreenfingersAI2 — Java ai/instance/esoterrace/GreenfingersAI2.java. Escort helper that casts a
// support skill on the esoterrace boss once it reaches its recorded waypoint.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("greenfingers")]
public sealed class GreenfingersAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java recorded a per-npcId walk waypoint index + "helper" skill here (282176->24/19271,
        // 282177->26/18751, 282178->40/16634) for handleMoveArrived to compare against
        // MoveController.getCurrentPoint(); walk-point tracking isn't exposed to the script layer yet
        // (also overrode canThink() to stay always-inactive — no C# equivalent).
    }

    public override void OnMoveArrived()
    {
        base.OnMoveArrived();
        // note: Java stopped walking (WalkManager.stopWalking) once the recorded waypoint was reached,
        // cast the helper skill at the esoterrace boss (npcId 217185) via SkillEngine, then despawned
        // the owner (AI2Actions.deleteOwner) after a 3s delay. WalkManager/waypoint tracking, boss
        // lookup by npcId, and scripted despawn aren't exposed to the script layer yet.
    }
}
