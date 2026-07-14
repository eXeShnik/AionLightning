using System.Linq;
using System.Collections.Generic;
using System;
// UnstableDayshadeAI2 — Java ai/instance/unstableSplinterpath/UnstableDayshadeAI2.java. Trash gate
// that, on first attack, silently dies and spawns the Ebonsoul/Rukril bosses.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("unstabledayshade")]
public sealed class UnstableDayshadeAI2 : AggressiveNpcAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: Java died silently via AI2Actions.dieSilently, spawned Ebonsoul (219940) and Rukril
        // (219939), and deleted itself via AI2Actions.deleteOwner on first attack. AI2Actions has no
        // ported equivalent.
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        // note: Java only reset its local "already triggered" flag here.
    }
}
