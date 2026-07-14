// DebilkarimTheMakerAI2Lower — Java ai/instance/lowerUdasTemple/DebilkarimTheMakerAI2.java. Alternate/
// lower Uda's Temple variant of the Debilkarim the Maker boss. Renamed to
// DebilkarimTheMakerAI2Lower to avoid a class-name collision with
// udasTempleLower/DebilkarimTheMakerAI2.java — both Java classes carry the verbatim @AIName
// "debilkarimthemaker" and would otherwise collide in this port's flat `Ai` namespace; see
// ChuraTwinbladeAI2Lower for the identical situation.
using System;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("debilkarimthemaker")]
public sealed class DebilkarimTheMakerAI2Lower : AggressiveNpcAI2
{
    private bool _isStart;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        int hp = Owner.HpPercentage;
        if (hp <= 75) _isStart = true;
        // note: Java cast Rockfall (18633)/Wave Of Fear (18635)/Invoke Energy (18636)/Infernal Rift (19000)
        // via AI2Actions across the 75-50/50-25/25-10% bands; AI2Actions skill-casting isn't wired at the
        // script layer yet.
        if (hp <= 10 && _isStart && !Owner.IsAlreadyDead)
        {
            for (int i = 0; i < 2; i++)
            {
                int distance = Random.Shared.Next(3, 11);
                double direction = Random.Shared.Next(0, 200) / 100.0;
                float x = (float)(Math.Cos(Math.PI * direction) * distance);
                float y = (float)(Math.Sin(Math.PI * direction) * distance);
                Spawn(217165, Owner.Position.X + x, Owner.Position.Y + y, Owner.Position.Z);
            }
        }
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        _isStart = false;
    }

    public override AttackIntention ChooseAttackIntention()
        // note: Java picked SWITCH_TARGET/SKILL_ATTACK/FINISH_ATTACK via AggroList.getMostHated() and
        // SkillAttackManager.chooseNextSkill(); aggro tracking and skill-attack selection are owned by
        // NpcAiService, not this script layer.
        => AttackIntention.SimpleAttack;
}
