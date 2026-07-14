using System.Linq;
using System.Collections.Generic;
using System;
// PieceOfSplendorAI2 — Java ai/instance/unstableSplinterpath/PieceOfSplendorAI2.java. Puzzle piece:
// while Ebonsoul carries a stacked debuff and Rukril is within range, clears both debuffs.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("pieceofsplendor")]
public sealed class PieceOfSplendorAI2 : AggressiveNpcAI2
{
    public override void OnCreatureSee(Creature creature) => CheckDistance(creature);

    public override void OnCreatureMoved(Creature creature) => CheckDistance(creature);

    private void CheckDistance(Creature creature)
    {
        if (creature is not Npc) return;
        var rukril = GetNpc(219939);
        var ebonsoul = GetNpc(219940);
        if (ebonsoul is null) return;
        if (Owner.Position.DistanceTo(ebonsoul.Position) <= 5 &&
            ebonsoul.GetActiveEffects().Any(e => e.SkillId == 19159))
        {
            ebonsoul.RemoveEffectBySkillId(19159);
            if (rukril is not null && rukril.GetActiveEffects().Any(e => e.SkillId == 19266))
                rukril.RemoveEffectBySkillId(19266);
        }
    }
}
