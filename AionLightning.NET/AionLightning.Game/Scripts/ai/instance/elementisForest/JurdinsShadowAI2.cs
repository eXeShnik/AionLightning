using System.Linq;
using System.Collections.Generic;
using System;
// JurdinsShadowAI2 — Java ai/instance/elementisForest/JurdinsShadowAI2.java. Shadow add: casts a
// self skill immediately on spawn.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("jurdinshadow")]
public sealed class JurdinsShadowAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        UseSkill(19404);
    }
}
