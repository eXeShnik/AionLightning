using System.Linq;
using System.Collections.Generic;
using System;
// KinquidDebuffAI2 — Java ai/instance/tallocsHollow/KinquidDebuffAI2.java. Destroyer add: casts a
// gear-destruction debuff on a nearby Kinquid when it moves close.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("kinquid_debuff")]
public sealed class KinquidDebuffAI2 : AggressiveNpcAI2
{
    public override void OnCreatureMoved(Creature creature)
    {
        base.OnCreatureMoved(creature);
        // note: Java checked isInRange(creature, 2) and creature.getNpcId() == 215467 before casting
        // skill 19235/19236 (chosen by this add's own npc id, 282008 vs 282009); 3d-range checks aren't
        // exposed to scripts yet.
    }
}
