using System.Linq;
using System.Collections.Generic;
using System;
// PieceOfMidnightAI2 — Java ai/instance/unstableSplinterpath/PieceOfMidnightAI2.java. Puzzle piece:
// while Rukril carries a stacked debuff and Ebonsoul is within range, clears both debuffs.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("pieceofmidnight")]
public sealed class PieceOfMidnightAI2 : AggressiveNpcAI2
{
    public override void OnCreatureSee(Creature creature) => CheckDistance(creature);

    public override void OnCreatureMoved(Creature creature) => CheckDistance(creature);

    private void CheckDistance(Creature creature)
    {
        if (creature is not Npc) return;
        var rukril = GetNpc(219939);
        var ebonsoul = GetNpc(219940);
        if (rukril is null) return;
        if (Owner.Position.DistanceTo(rukril.Position) <= 5 &&
            rukril.GetActiveEffects().Any(e => e.SkillId == 19266))
        {
            rukril.RemoveEffectBySkillId(19266);
            if (ebonsoul is not null && ebonsoul.GetActiveEffects().Any(e => e.SkillId == 19159))
                ebonsoul.RemoveEffectBySkillId(19159);
        }
    }
}
