// BarrelAI2 — Java ai/instance/crucibleChallenge/BarrelAI2.java. Destructible barrel prop: on death,
// spawns its broken variant at a random offset and deletes itself.
using System;
using AionLightning.Game.Ai;

namespace Ai;

[AiName("barrel")]
public sealed class BarrelAI2 : NpcAi2
{
    public override void OnDied()
    {
        base.OnDied();
        int npcId = getOwner().Template.NpcId switch
        {
            218560 => 218561,
            217840 => 217841,
            _ => 0
        };
        double direction = Math.PI * (Random.Shared.Next(0, 200) / 100.0);
        var pos = getOwner().Position;
        float x = pos.X + (float)(Math.Cos(direction) * 4);
        float y = pos.Y + (float)(Math.Sin(direction) * 4);
        Spawn(npcId, x, y, pos.Z);
        // note: Java also deleted itself via AI2Actions.deleteOwner; AI2Actions isn't exposed to scripts yet.
    }
}
